using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.Services;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using scrm_dev_mvc.Models;
using System;

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

        public async Task<bool> UpdateDealStageAsync(int dealId, string newStageName)
        {
            try
            {
                // 1. Find the new Stage ID from its name
                var newStage = await _unitOfWork.Stages.FirstOrDefaultAsync(
                    s => s.Name == newStageName
                );

                if (newStage == null)
                {
                    _logger.LogWarning("UpdateDealStageAsync: Stage '{StageName}' not found.", newStageName);
                    throw new KeyNotFoundException($"Stage '{newStageName}' not found.");
                }

                // 2. Find the Deal
                var deal = await _unitOfWork.Deals.FirstOrDefaultAsync(d => d.Id == dealId);
                if (deal == null || deal.IsDeleted == true)
                {
                    _logger.LogWarning("UpdateDealStageAsync: Deal {DealId} not found.", dealId);
                    throw new KeyNotFoundException($"Deal with ID {dealId} not found.");
                }

                // 3. Check if a change is needed
                if (deal.StageId == newStage.Id)
                {
                    _logger.LogInformation("Deal {DealId} is already in Stage {StageName}. No update needed.", dealId, newStageName);
                    return true; // No change, but operation is "successful"
                }

                // 4. Update the StageId
                deal.StageId = newStage.Id;
                _unitOfWork.Deals.Update(deal);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Updated Deal {DealId} to Stage {StageId} ({StageName})", deal.Id, newStage.Id, newStage.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating deal stage for Deal ID {DealId}", dealId);
                return false; // Return false to indicate failure
            }
        }

        // You would also move your KanbanBoard data-fetching logic here
    }
}
