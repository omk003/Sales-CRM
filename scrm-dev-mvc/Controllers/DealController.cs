using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.services.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace scrm_dev_mvc.Controllers
{
    [Authorize]
    public class DealController : Controller
    {
        private readonly IDealService _dealService;
        private readonly ILogger<DealController> _logger;
        private readonly ICurrentUserService _currentUserService;
        public DealController(IDealService dealService, ILogger<DealController> logger, ICurrentUserService currentUserService)
        {
            _dealService = dealService;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<IActionResult> KanbanBoard()
        {
            try
            {
                var userd = _currentUserService.GetUserId();
                var viewModel = await _dealService.GetKanbanBoardAsync(userd);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Kanban Board");
                return View(new KanbanBoardViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? companyId, int? contactId)
        {
            var vm = await _dealService.GetCreateFormAsync(companyId);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Insert(DealFormViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state in Insert Deal");
                var refreshedVm = await _dealService.GetCreateFormAsync(vm.Deal.CompanyId);
                return View("Create", refreshedVm);
            }

            try
            {
                var userId = _currentUserService.GetUserId();
                if (userId == Guid.Empty)
                {
                    _logger.LogWarning("Invalid or missing user ID in Insert Deal");
                    return Unauthorized();
                }

                bool success = await _dealService.InsertDealAsync(vm, userId);
                if (success)
                {
                    _logger.LogInformation("New deal inserted successfully by {UserId}", userId);
                    return RedirectToAction("KanbanBoard");
                }

                _logger.LogWarning("Deal insertion failed for user {UserId}", userId);
                return View("Create", vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting deal");
                ModelState.AddModelError(string.Empty, "Unexpected error occurred while creating the deal.");
                return View("Create", vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var vm = await _dealService.GetUpdateFormAsync(id);
            if (vm == null)
            {
                _logger.LogWarning("Deal {DealId} not found for update", id);
                return NotFound();
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(DealFormViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state in Update Deal {DealId}", vm.Deal.Id);
                return View("Update", vm);
            }

            try
            {
                bool updated = await _dealService.UpdateDealAsync(vm);
                if (!updated)
                {
                    _logger.LogWarning("Deal update failed for DealId {DealId}", vm.Deal.Id);
                    return NotFound();
                }

                _logger.LogInformation("Deal updated successfully. ID: {DealId}", vm.Deal.Id);
                return RedirectToAction("KanbanBoard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating deal {DealId}", vm.Deal.Id);
                ModelState.AddModelError(string.Empty, "Unexpected error occurred while updating the deal.");
                return View("Update", vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var vm = await _dealService.GetDealDetailsAsync(id);
            if (vm == null)
            {
                _logger.LogWarning("Deal details not found for DealId {DealId}", id);
                return NotFound();
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDealStage([FromBody] UpdateDealStageRequest request)
        {
            if (request == null || request.DealId <= 0 || string.IsNullOrWhiteSpace(request.NewStageName))
            {
                _logger.LogWarning("Invalid UpdateDealStage request: {@Request}", request);
                return BadRequest(new { success = false, message = "Invalid request data." });
            }

            try
            {
                bool success = await _dealService.UpdateDealStageAsync(request.DealId, request.NewStageName);

                if (success)
                {
                    _logger.LogInformation("Deal {DealId} moved to stage {StageName}", request.DealId, request.NewStageName);
                    return Json(new { success = true, message = "Deal stage updated successfully." });
                }

                _logger.LogWarning("Deal stage update failed for Deal {DealId}", request.DealId);
                return StatusCode(500, new { success = false, message = "Failed to update deal stage." });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Stage or Deal not found: {@Request}", request);
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating deal stage");
                return StatusCode(500, new { success = false, message = "Unexpected error occurred." });
            }
        }
    }

    public class UpdateDealStageRequest
    {
        public int DealId { get; set; }
        public string NewStageName { get; set; }
    }
}
