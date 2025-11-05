using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.services; // Assuming this is where IWorkspaceService is
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace scrm_dev_mvc.Services
{
    public class WorkspaceService : IWorkspaceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<WorkspaceService> _logger;

        public WorkspaceService(IUnitOfWork unitOfWork, ILogger<WorkspaceService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<WorkspaceViewModel> GetWorkspaceDataAsync(Guid userId, bool isAdmin)
        {
            var viewModel = new WorkspaceViewModel { IsAdmin = isAdmin };
            var today = DateTime.UtcNow;

            // --- 1. GET TASKS ---
            try
            {
                var completedStatus = await _unitOfWork.TaskStatuses.FirstOrDefaultAsync(s => s.StatusName == "Completed");
                int completedStatusId = completedStatus?.Id ?? -1;

                // --- Query 1: Get INCOMPLETE Tasks ---
                var incompleteTasks = await _unitOfWork.Tasks.GetAllAsync(
                    t => t.OwnerId == userId && t.StatusId != completedStatusId,
                    false,
                    t => t.Contact,
                    t => t.Deal
                );

                var taskSummaries = incompleteTasks.Select(t => new TaskSummaryViewModel
                {
                    Id = t.Id,
                    Title = t.Title ?? "N/A",
                    DueDate = t.DueDate ?? today.AddYears(1),
                    RelatedTo = t.ContactId.HasValue ? $"{t.Contact?.FirstName} {t.Contact?.LastName} (Contact)" :
                                t.DealId.HasValue ? $"{t.Deal?.Name} (Deal)" : "No Association"
                });

                viewModel.PendingTasks = taskSummaries
                    .Where(t => t.DueDate.Date >= today.Date)
                    .OrderBy(t => t.DueDate)
                    .ToList();

                viewModel.DueTasks = taskSummaries
                    .Where(t => t.DueDate.Date < today.Date)
                    .OrderBy(t => t.DueDate)
                    .ToList();

                // --- Query 2: Get COMPLETED Tasks (e.g., last 50) ---
                var completedTasks = await _unitOfWork.Tasks.GetAllAsync(
                    predicate: t => t.OwnerId == userId && t.StatusId == completedStatusId,
                    asNoTracking: false,
                    t => t.Contact,
                    t => t.Deal
                );

                viewModel.CompletedTasks = completedTasks
                    .OrderByDescending(t => t.CompletedAt ?? t.DueDate) // Order by when they were completed
                    .Take(50) // Only show the last 50
                    .Select(t => new TaskSummaryViewModel
                    {
                        Id = t.Id,
                        Title = t.Title ?? "N/A",
                        DueDate = t.DueDate ?? today, // Use 'today' as a fallback
                        RelatedTo = t.ContactId.HasValue ? $"{t.Contact?.FirstName} {t.Contact?.LastName} (Contact)" :
                                    t.DealId.HasValue ? $"{t.Deal?.Name} (Deal)" : "No Association"
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user tasks for workspace.");
            }

            // --- 2. Get "My Activity" Data (No changes here) ---
            try
            {
                var myActivities = await _unitOfWork.Activities.GetAllAsync(
                    a => a.OwnerId == userId && a.ActivityDate >= today.AddDays(-30),
                    false,
                    a => a.ActivityType
                );

                var groupedActivities = myActivities
                    .GroupBy(a => a.ActivityType.Name.ToLower())
                    .ToDictionary(
                        g => g.Key,
                        g => g.GroupBy(a => a.ActivityDate.Value.Date)
                              .Select(dateGroup => new ActivityChartDataPoint
                              {
                                  Date = dateGroup.Key.ToString("yyyy-MM-dd"),
                                  Count = dateGroup.Count()
                              })
                              .OrderBy(dp => dp.Date)
                              .ToList()
                    );

                viewModel.MyActivityData["tasks"] = groupedActivities.GetValueOrDefault("task", new List<ActivityChartDataPoint>());
                viewModel.MyActivityData["emails"] = groupedActivities.GetValueOrDefault("email", new List<ActivityChartDataPoint>());
                viewModel.MyActivityData["calls"] = groupedActivities.GetValueOrDefault("call", new List<ActivityChartDataPoint>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user activity for workspace chart.");
            }


            // --- 3. Get "Team Activity" Data if Admin (No changes here) ---
            if (isAdmin)
            {
                try
                {
                    // --- STEP 3a: Get the Admin's Organization ID ---
                    // (Assuming your user model is ApplicationUser and is on the UoW)
                    var adminUser = await _unitOfWork.Users.FindAsync(userId);
                    if (adminUser == null || !adminUser.OrganizationId.HasValue)
                    {
                        _logger.LogWarning("Admin user {UserId} not found or has no OrganizationId.", userId);
                        return viewModel; // Return the view model with just the user's data
                    }

                    var organizationId = adminUser.OrganizationId.Value;

                    // --- STEP 3b: Get all User IDs in that Organization ---
                    var orgUserIds = (await _unitOfWork.Users.GetAllAsync(
                            u => u.OrganizationId == organizationId,
                            true // asNoTracking
                        ))
                        .Select(u => u.Id)
                        .ToList();

                    // --- STEP 3c: Query Activities ONLY for those users ---
                    var teamActivities = await _unitOfWork.Activities.GetAllAsync(
                        a => a.ActivityDate >= today.AddDays(-30) &&
                             orgUserIds.Contains(a.OwnerId), 
                        false,
                        a => a.ActivityType,
                        a => a.Owner
                    );


                    var groupedTeamActivities = teamActivities
                        .Where(a => a.Owner != null)
                        .GroupBy(a => a.ActivityType.Name.ToLower())
                        .ToDictionary(
                            g => g.Key,
                            g => g.GroupBy(a => a.Owner.Email) 
                                  .Select(userGroup => new UserActivityStats
                                  {
                                      User = userGroup.Key,
                                      Count = userGroup.Count()
                                  })
                                  .OrderByDescending(us => us.Count)
                                  .ToList()
                        );

                    viewModel.TeamActivityData["tasks"] = groupedTeamActivities.GetValueOrDefault("task", new List<UserActivityStats>());
                    viewModel.TeamActivityData["emails"] = groupedTeamActivities.GetValueOrDefault("email", new List<UserActivityStats>());
                    viewModel.TeamActivityData["calls"] = groupedTeamActivities.GetValueOrDefault("call", new List<UserActivityStats>());
                    //var teamActivities = await _unitOfWork.Activities.GetAllAsync(
                    //    a => a.ActivityDate >= today.AddDays(-30),
                    //    false,
                    //    a => a.ActivityType,
                    //    a => a.Owner
                    //);

                    //var groupedTeamActivities = teamActivities
                    //    .Where(a => a.Owner != null)
                    //    .GroupBy(a => a.ActivityType.Name.ToLower())
                    //    .ToDictionary(
                    //        g => g.Key,
                    //        g => g.GroupBy(a => a.Owner.FirstName) // Group by user
                    //              .Select(userGroup => new UserActivityStats
                    //              {
                    //                  User = userGroup.Key,
                    //                  Count = userGroup.Count()
                    //              })
                    //              .OrderByDescending(us => us.Count)
                    //              .ToList()
                    //    );

                    //viewModel.TeamActivityData["tasks"] = groupedTeamActivities.GetValueOrDefault("task", new List<UserActivityStats>());
                    //viewModel.TeamActivityData["emails"] = groupedTeamActivities.GetValueOrDefault("email", new List<UserActivityStats>());
                    //viewModel.TeamActivityData["calls"] = groupedTeamActivities.GetValueOrDefault("call", new List<UserActivityStats>());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching team activity for admin workspace.");
                }
            }

            return viewModel;
        }
    }
}