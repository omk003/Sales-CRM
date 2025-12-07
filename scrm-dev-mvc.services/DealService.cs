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
using scrm_dev_mvc.Models.Enums;

namespace scrm_dev_mvc.services
{
    public class DealService : IDealService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DealService> _logger;
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUserService;

        public DealService(IUnitOfWork unitOfWork, ILogger<DealService> logger, IUserService userService, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userService = userService;
            _currentUserService = currentUserService;
        }


        public async Task<KanbanBoardViewModel> GetKanbanBoardAsync(Guid ownerId)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(ownerId);
                if (user == null)
                {
                    _logger.LogWarning("GetKanbanBoardAsync: User with ID {UserId} not found.", ownerId);
                    return new KanbanBoardViewModel
                    {
                        DealStages = new List<string>(),
                        DealsByStage = new Dictionary<string, List<Deal>>(),
                        StageTotals = new Dictionary<string, decimal>()
                    };
                }

                var stages = await _unitOfWork.Stages.GetAllAsync(s => true, asNoTracking: true);
                var deals = new List<Deal>();

                if (user.RoleId == (int)UserRoleEnum.SalesAdminSuper || user.RoleId == (int)UserRoleEnum.SalesAdmin)
                {
                    deals = await _unitOfWork.Deals.GetAllAsync(
                       d => d.IsDeleted != true && d.OrganizationId == user.OrganizationId,
                       asNoTracking: true,
                        d => d.Stage, d => d.Owner, d => d.Company
                   );
                }
                else
                {
                    deals = await _unitOfWork.Deals.GetAllAsync(
                    d => d.IsDeleted != true && d.OwnerId == ownerId,
                    asNoTracking: true,
                     d => d.Stage, d => d.Owner, d => d.Company
                    );
                }



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


        public async Task<DealFormViewModel> GetCreateFormAsync(int? companyId)
        {
            var userId = _currentUserService.GetUserId();
            var userFromDb = await _userService.GetUserByIdAsync(userId);
            List<Company> CompaniesFromDb;

            if (userFromDb.RoleId == 2)
            {
                CompaniesFromDb = await _unitOfWork.Company.GetAllAsync(c => c.IsDeleted == false && c.OrganizationId == userFromDb.OrganizationId, asNoTracking: true);
            }
            else
            {
                CompaniesFromDb = await _unitOfWork.Company.GetAllAsync(c => c.IsDeleted == false && c.OrganizationId == userFromDb.OrganizationId && c.UserId == userId, asNoTracking: true);
            }
            var vm = new DealFormViewModel
            {
                Deal = new Deal
                {
                    CompanyId = companyId,
                    CloseDate = DateTime.UtcNow.AddMonths(1)
                },
                Users = await _unitOfWork.Users.GetAllAsync(u => u.IsDeleted == false && u.OrganizationId == userFromDb.OrganizationId, asNoTracking: true),
                Stages = await _unitOfWork.Stages.GetAllAsync(s => true, asNoTracking: true),
                Companies = CompaniesFromDb
            };

            return vm;
        }

        public async Task<bool> InsertDealAsync(DealFormViewModel vm, Guid userId)
        {

            try
            {
                var user = await _userService.GetUserByIdAsync(userId);
                vm.Deal.OwnerId = vm.Deal.OwnerId;
                vm.Deal.CreatedAt = DateTime.UtcNow;
                vm.Deal.IsDeleted = false;
                vm.Deal.OrganizationId = user.OrganizationId ?? 0;

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
                var dealFromDb = await _unitOfWork.Deals.FirstOrDefaultAsync(u => u.Id == vm.Deal.Id, "Contacts");
                if (dealFromDb == null || dealFromDb.IsDeleted == true)
                    return false;
                List<Contact> contactListDeal = dealFromDb.Contacts.ToList();
                if(contactListDeal.Count == 0)
                {
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
                else
                {
                    var companyIdFromContact = contactListDeal[0].CompanyId;
                    if(companyIdFromContact != vm.Deal.CompanyId)
                    {
                        return false;
                    }
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
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating deal ID {DealId}", vm.Deal.Id);
                return false;
            }
        }


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


        private async System.Threading.Tasks.Task RepopulateDealFormAsync(DealFormViewModel vm)
        {
            var userId = _currentUserService.GetUserId();
            var userFromDb = await _userService.GetUserByIdAsync(userId);

            vm.Users = await _unitOfWork.Users.GetAllAsync(u => u.IsDeleted == false && u.OrganizationId == userFromDb.OrganizationId, asNoTracking: true);
            vm.Stages = await _unitOfWork.Stages.GetAllAsync(s => true, asNoTracking: true);
            vm.Companies = await _unitOfWork.Company.GetAllAsync(c => c.IsDeleted == false && c.OrganizationId == userFromDb.OrganizationId, asNoTracking: true);
        }


        public async Task<bool> DeleteDealAsync(int dealId)
        {
            try
            {
                var deal = await _unitOfWork.Deals.FirstOrDefaultAsync(d => d.Id == dealId);
                if (deal == null || deal.IsDeleted == true)
                {
                    _logger.LogWarning("DeleteDealAsync: Deal {DealId} not found.", dealId);
                    return false;
                }
                deal.IsDeleted = true;
                _unitOfWork.Deals.Update(deal);
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Deleted Deal {DealId}", dealId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting deal ID {DealId}", dealId);
                return false;
            }
        }

        public async Task<bool> AssociateCompanyAsync(int dealId, int companyId)
        {
            var dealFromDb = await _unitOfWork.Deals.FirstOrDefaultAsync(d => d.Id == dealId, "Contacts");
            if (dealFromDb == null || dealFromDb.IsDeleted == true)
            {
                _logger.LogWarning("AssociateCompanyAsync: Deal {DealId} not found.", dealId);
                return false;
            }
            List<Contact> contacts = dealFromDb.Contacts.ToList();
            if(contacts.Count != 0)
            {
                var companyIdFromContact = contacts[0].CompanyId;
                if(companyIdFromContact != companyId)
                {
                    return false;
                }
                else
                {
                    dealFromDb.CompanyId = companyId;
                }
            }
            else
            {
                dealFromDb.CompanyId = companyId;
            }
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DisassociateCompanyAsync(int dealId)
        {
            var dealFromDb = await _unitOfWork.Deals.FirstOrDefaultAsync(d => d.Id == dealId);

            dealFromDb.CompanyId = null;
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
