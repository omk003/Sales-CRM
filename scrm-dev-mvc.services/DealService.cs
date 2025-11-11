using Microsoft.EntityFrameworkCore;
using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace scrm_dev_mvc.services
{
    public class DealService : IDealService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DealService> _logger;

        public DealService(IUnitOfWork unitOfWork, ILogger<DealService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // =======================
        // 1️⃣ KANBAN BOARD
        // =======================
        public async Task<KanbanBoardViewModel> GetKanbanBoardAsync()
        {
            try
            {
                var stages = await _unitOfWork.Stages.GetAllAsync(s => true, asNoTracking: true);
                var deals = await _unitOfWork.Deals.GetAllAsync(
                    d => d.IsDeleted != true,
                    asNoTracking: true,
                     d => d.Stage, d => d.Owner, d => d.Company
                );

                var dealsByStage = stages.ToDictionary(
                    stage => stage.Name,
                    stage => deals.Where(d => d.StageId == stage.Id).ToList()
                );

                var stageTotals = stages.ToDictionary(
                    stage => stage.Name,
                    stage => deals.Where(d => d.StageId == stage.Id)
                                  .Sum(d => d.Value ?? 0)
                );

                return new KanbanBoardViewModel
                {
                    DealStages = stages.Select(s => s.Name).ToList(),
                    DealsByStage = dealsByStage,
                    StageTotals = stageTotals
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Kanban board.");
                return new KanbanBoardViewModel
                {
                    DealStages = new List<string>(),
                    DealsByStage = new Dictionary<string, List<Deal>>(),
                    StageTotals = new Dictionary<string, decimal>()
                };
            }
        }

        // =======================
        // 2️⃣ CREATE DEAL
        // =======================
        public async Task<DealFormViewModel> GetCreateFormAsync(int? companyId)
        {
            var vm = new DealFormViewModel
            {
                Deal = new Deal
                {
                    CompanyId = companyId,
                    CloseDate = DateTime.UtcNow.AddMonths(1)
                },
                Users = await _unitOfWork.Users.GetAllAsync(u => true, asNoTracking: true),
                Stages = await _unitOfWork.Stages.GetAllAsync(s => true, asNoTracking: true),
                Companies = await _unitOfWork.Company.GetAllAsync(c => c.IsDeleted == false, asNoTracking: true)
            };

            return vm;
        }

        public async Task<bool> InsertDealAsync(DealFormViewModel vm, Guid userId)
        {
            try
            {
                vm.Deal.OwnerId = vm.Deal.OwnerId ?? userId;
                vm.Deal.CreatedAt = DateTime.UtcNow;
                vm.Deal.IsDeleted = false;

                await _unitOfWork.Deals.AddAsync(vm.Deal);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("New deal created. ID: {DealId}", vm.Deal.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating new deal.");
                return false;
            }
        }

        // =======================
        // 3️⃣ UPDATE DEAL
        // =======================
        public async Task<DealFormViewModel?> GetUpdateFormAsync(int id)
        {
            var deal = await _unitOfWork.Deals.FirstOrDefaultAsync(u => u.Id == id);
            if (deal == null || deal.IsDeleted == true)
                return null;

            var vm = new DealFormViewModel { Deal = deal };
            await RepopulateDealFormAsync(vm);
            return vm;
        }

        public async Task<bool> UpdateDealAsync(DealFormViewModel vm)
        {
            try
            {
                var dealFromDb = await _unitOfWork.Deals.FirstOrDefaultAsync(u => u.Id == vm.Deal.Id);
                if (dealFromDb == null || dealFromDb.IsDeleted == true)
                    return false;

                dealFromDb.Name = vm.Deal.Name;
                dealFromDb.Value = vm.Deal.Value;
                dealFromDb.StageId = vm.Deal.StageId;
                dealFromDb.CompanyId = vm.Deal.CompanyId;
                dealFromDb.OwnerId = vm.Deal.OwnerId;
                dealFromDb.CloseDate = vm.Deal.CloseDate;

                _unitOfWork.Deals.Update(dealFromDb);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Deal updated. ID: {DealId}", vm.Deal.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating deal ID {DealId}", vm.Deal.Id);
                return false;
            }
        }

        // =======================
        // 4️⃣ DETAILS
        // =======================
        public async Task<DealPreviewViewModel?> GetDealDetailsAsync(int id)
        {
            var deal = await _unitOfWork.Deals.FirstOrDefaultAsync(
                d => d.Id == id && d.IsDeleted != true,
                include: "Owner,Stage,Company,Contacts"
            );

            if (deal == null)
                return null;

            var activities = await _unitOfWork.Activities.GetAllAsync(
                a => a.DealId == id,
                asNoTracking: true,
                includes: a => a.ActivityType
            );

            var vm = new DealPreviewViewModel
            {
                Deal = deal,
                Activities = activities.OrderByDescending(a => a.ActivityDate),
                Contacts = deal.Contacts ?? new List<Contact>()
            };

            return vm;
        }

        // =======================
        // 5️⃣ GET ALL DEALS (for DataTable)
        // =======================
        public async Task<IEnumerable<object>> GetAllDealsForUserAsync(Guid userId)
        {
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || !user.OrganizationId.HasValue)
                return new List<object>();

            int orgId = user.OrganizationId.Value;

            var deals = await _unitOfWork.Deals.GetAllAsync(
                d => d.IsDeleted != true &&
                     d.CompanyId != null &&
                     d.Company.OrganizationId == orgId,
                asNoTracking: true,
                includes: d => d.Company
            );

            return deals.Select(d => new { d.Id, d.Name, d.Value });
        }

        // =======================
        // 6️⃣ UPDATE DEAL STAGE (Your Original Method)
        // =======================
        public async Task<bool> UpdateDealStageAsync(int dealId, string newStageName)
        {
            try
            {
                var newStage = await _unitOfWork.Stages.FirstOrDefaultAsync(
                    s => s.Name == newStageName
                );

                if (newStage == null)
                {
                    _logger.LogWarning("UpdateDealStageAsync: Stage '{StageName}' not found.", newStageName);
                    throw new KeyNotFoundException($"Stage '{newStageName}' not found.");
                }

                var deal = await _unitOfWork.Deals.FirstOrDefaultAsync(d => d.Id == dealId);
                if (deal == null || deal.IsDeleted == true)
                {
                    _logger.LogWarning("UpdateDealStageAsync: Deal {DealId} not found.", dealId);
                    throw new KeyNotFoundException($"Deal with ID {dealId} not found.");
                }

                if (deal.StageId == newStage.Id)
                {
                    _logger.LogInformation("Deal {DealId} already in Stage {StageName}.", dealId, newStageName);
                    return true;
                }

                deal.StageId = newStage.Id;
                _unitOfWork.Deals.Update(deal);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Updated Deal {DealId} to Stage {StageId} ({StageName})",
                    deal.Id, newStage.Id, newStage.Name);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating deal stage for Deal ID {DealId}", dealId);
                return false;
            }
        }

        // =======================
        // 🔧 HELPER
        // =======================
        private async System.Threading.Tasks.Task RepopulateDealFormAsync(DealFormViewModel vm)
        {
            vm.Users = await _unitOfWork.Users.GetAllAsync(u => true, asNoTracking: true);
            vm.Stages = await _unitOfWork.Stages.GetAllAsync(s => true, asNoTracking: true);
            vm.Companies = await _unitOfWork.Company.GetAllAsync(c => c.IsDeleted == false, asNoTracking: true);
        }
    }
}
