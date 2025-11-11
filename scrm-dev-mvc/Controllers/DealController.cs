using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // For .Include()
using scrm_dev_mvc.Data.Repository;
using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.services.Interfaces;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace scrm_dev_mvc.Controllers
{
    [Authorize]
    public class DealController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DealController> _logger;
        public readonly IDealService _dealService;

        // Inject IUnitOfWork and ILogger
        public DealController(IUnitOfWork unitOfWork, ILogger<DealController> logger, IDealService dealService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _dealService = dealService;
        }

        // --- KANBAN BOARD (Refactored to use real data) ---
        public async Task<IActionResult> KanbanBoard()
        {
            try
            {
                var stages = await _unitOfWork.Stages.GetAllAsync(s => true, asNoTracking: true);
                var deals = await _unitOfWork.Deals.GetAllAsync(
                    d => d.IsDeleted != true,
                    asNoTracking: true,
                    includes:
                    [
                        d => d.Stage,
                        d => d.Owner,
                        d => d.Company
                    ]
                );

                // Group Deals by Stage Name
                var dealsByStage = stages.ToDictionary(
                    stage => stage.Name,
                    stage => deals.Where(d => d.StageId == stage.Id).ToList()
                );

                // --- NEW: Calculate Totals ---
                var stageTotals = stages.ToDictionary(
                    stage => stage.Name,
                    stage => deals
                        .Where(d => d.StageId == stage.Id)
                        .Sum(d => d.Value ?? 0) // Sum values, treating null as 0
                );
                // --- END NEW ---

                var vm = new KanbanBoardViewModel
                {
                    DealStages = stages.Select(s => s.Name).ToList(),
                    DealsByStage = dealsByStage,
                    StageTotals = stageTotals // Pass totals to the view
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading KanbanBoard");
                // Return empty board on error
                return View(new KanbanBoardViewModel
                {
                    DealStages = new List<string>(),
                    DealsByStage = new Dictionary<string, List<Deal>>(),
                    StageTotals = new Dictionary<string, decimal>() // Initialize empty
                });
            }
        }

        // --- CREATE (GET) ---
        // GET: /Deal/Create
        [HttpGet]
        public async Task<IActionResult> Create(int? companyId, int? contactId)
        {
            var vm = new DealFormViewModel
            {
                Deal = new Deal
                {
                    CompanyId = companyId,
                    CloseDate = DateTime.UtcNow.AddMonths(1) // Default close date
                },
                Users = await _unitOfWork.Users.GetAllAsync(u => true, asNoTracking: true),
                Stages = await _unitOfWork.Stages.GetAllAsync(s => true, asNoTracking: true),
                Companies = await _unitOfWork.Company.GetAllAsync(c => true, asNoTracking: true)
            };

            if (contactId.HasValue)
            {
                ViewBag.ContactId = contactId.Value;
            }

            return View(vm);
        }

        // --- CREATE (POST) - Renamed to 'Insert' to match your form ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Insert(DealFormViewModel vm, int? contactId)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Deal Insert: Model state is invalid.");
                await RepopulateDealFormViewModel(vm);
                return View("Create", vm);
            }

            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Guid.TryParse(userIdString, out Guid userId);

                // Assign Owner, CreatedAt, etc.
                vm.Deal.OwnerId = vm.Deal.OwnerId ?? userId; // Default to current user if not set
                vm.Deal.CreatedAt = DateTime.UtcNow;
                vm.Deal.IsDeleted = false;

                await _unitOfWork.Deals.AddAsync(vm.Deal);
                await _unitOfWork.SaveChangesAsync(); // Save to get the new Deal.Id

                // Handle associating the contact if one was passed
                //if (contactId.HasValue)
                //{
                //    var contact = await _unitOfWork.Contacts.FirstOrDefaultAsync(u => u.Id == contactId.Value);
                //    if (contact != null)
                //    {
                //        contact.DealId = vm.Deal.Id; // Assuming Contact has a nullable DealId
                //        _unitOfWork.Contacts.Update(contact);
                //        await _unitOfWork.SaveAsync();
                //    }
                //}

                _logger.LogInformation("New deal created. ID: {DealId}", vm.Deal.Id);
                return RedirectToAction("KanbanBoard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while inserting new deal.");
                await RepopulateDealFormViewModel(vm);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View("Create", vm);
            }
        }

        // --- UPDATE (GET) ---
        // GET: /Deal/Update/{id}
        public async Task<IActionResult> Update(int id)
        {
            var deal = await _unitOfWork.Deals.FirstOrDefaultAsync(u => u.Id == id);
            if (deal == null || deal.IsDeleted == true)
            {
                _logger.LogWarning("Deal Update (GET): Deal with ID {DealId} not found.", id);
                return NotFound();
            }

            var vm = new DealFormViewModel { Deal = deal };
            await RepopulateDealFormViewModel(vm);
            return View(vm);
        }

        // --- UPDATE (POST) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(DealFormViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Deal Update (POST): Model state is invalid for Deal ID {DealId}", vm.Deal.Id);
                await RepopulateDealFormViewModel(vm);
                return View("Update", vm);
            }

            try
            {
                var dealFromDb = await _unitOfWork.Deals.FirstOrDefaultAsync(u => u.Id == vm.Deal.Id);
                if (dealFromDb == null || dealFromDb.IsDeleted == true)
                {
                    _logger.LogWarning("Deal Update (POST): Deal with ID {DealId} not found in DB.", vm.Deal.Id);
                    return NotFound();
                }

                // Map updated properties
                dealFromDb.Name = vm.Deal.Name;
                dealFromDb.Value = vm.Deal.Value;
                dealFromDb.StageId = vm.Deal.StageId;
                dealFromDb.CompanyId = vm.Deal.CompanyId;
                dealFromDb.OwnerId = vm.Deal.OwnerId;
                dealFromDb.CloseDate = vm.Deal.CloseDate;

                _unitOfWork.Deals.Update(dealFromDb);
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Deal updated. ID: {DealId}", vm.Deal.Id);

                return RedirectToAction("KanbanBoard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating deal ID {DealId}", vm.Deal.Id);
                await RepopulateDealFormViewModel(vm);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View("Update", vm);
            }
        }

        // --- DETAILS PAGE (Corrected) ---
        // GET: /Deal/Details/5
        public async Task<IActionResult> Details(int id)
        {
            // 1. Get the Deal using FirstOrDefaultAsync (string-based includes)
            var deal = await _unitOfWork.Deals.FirstOrDefaultAsync(
                d => d.Id == id && d.IsDeleted != true,
                // Use string-based includes as per your repository
                include: "Owner,Stage,Company,Contacts"
            );

            if (deal == null)
            {
                _logger.LogWarning("Details(GET): Deal with ID {DealId} not found.", id);
                return NotFound();
            }

            // 2. Get associated activities (using Expression-based includes)
            var activities = await _unitOfWork.Activities.GetAllAsync(
                a => a.DealId == id,
                asNoTracking: true,
                includes: a => a.ActivityType
            );

            // 3. Get associated contacts (already loaded into the deal object)
            var contacts = deal.Contacts ?? new List<Contact>();

            // 4. Create the ViewModel
            var vm = new DealPreviewViewModel
            {
                Deal = deal,
                Activities = activities.OrderByDescending(a => a.ActivityDate),
                Contacts = contacts
            };

            return View(vm);
        }

        // In DealController.cs

    // In DealController.cs

[HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // 1. Get the current user's ID (GUID) from their claims.
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
        {
            _logger.LogWarning("GetAll: User has no valid 'NameIdentifier' claim.");
            return Json(new { data = new List<object>() });
        }

        // 2. Get the User object from the database
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u=>u.Id==userId); // Assuming GetAsync(Guid id) exists
        if (user == null || !user.OrganizationId.HasValue)
        {
            _logger.LogWarning("GetAll: User {UserId} not found or has no OrganizationId.", userId);
            return Json(new { data = new List<object>() });
        }

        int organizationId = user.OrganizationId.Value; // Get the OrganizationId

        // 3. Fetch deals, filtering by OrganizationId in the Company.
        var deals = await _unitOfWork.Deals.GetAllAsync(
            // The predicate now filters for deals where the Company's OrgID matches
            predicate: d => d.IsDeleted != true &&
                            d.CompanyId != null &&
                            d.Company.OrganizationId == organizationId,

            asNoTracking: true,

            // 4. Include the Company so the predicate (Where clause) can filter on it
            includes: d => d.Company
        );

        // Select only the properties the JS needs
        var result = deals.Select(d => new { d.Id, d.Name, d.Value });
        return Json(new { data = result });
    }

        // In DealController.cs

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDealStage([FromBody] UpdateDealStageRequest request) // <-- ADD [FromBody] HERE
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Now, 'request.DealId' and 'request.NewStageName' will have the correct values
            var success = await _dealService.UpdateDealStageAsync(request.DealId, request.NewStageName);

            if (success)
            {
                return Ok(new { message = "Deal stage updated successfully." });
            }
            else
            {
                return StatusCode(500, new { message = "An error occurred while updating the deal." });
            }
        }
        // Helper to fill dropdowns
        private async System.Threading.Tasks.Task RepopulateDealFormViewModel(DealFormViewModel vm)
        {
            vm.Users = await _unitOfWork.Users.GetAllAsync(u => true, asNoTracking: true);
            vm.Stages = await _unitOfWork.Stages.GetAllAsync(s => true, asNoTracking: true);
            vm.Companies = await _unitOfWork.Company.GetAllAsync(c => true, asNoTracking: true);
        }
    }
}

