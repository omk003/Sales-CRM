using Microsoft.Extensions.Logging;
using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.Models.DTO;
using scrm_dev_mvc.services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scrm_dev_mvc.services
{

    public class ActivityService : IActivityService
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly ILogger<ActivityService> _logger;

        public ActivityService(IUnitOfWork unitOfWork, ILogger<ActivityService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<scrm_dev_mvc.Models.Activity> CreateActivityAsync(CreateActivityDto activityDto)
            {
                if (string.IsNullOrWhiteSpace(activityDto.ActivityTypeName))
                {
                    throw new ArgumentException("ActivityTypeName must be provided.");
                }

            // 1. Find ActivityType using IUnitOfWork
            var activityType = await _unitOfWork.ActivityTypes.FirstOrDefaultAsync(
                    at => at.Name.ToLower() == activityDto.ActivityTypeName.ToLower()
                );

                if (activityType == null)
                {
                    throw new InvalidOperationException($"Activity type '{activityDto.ActivityTypeName}' not found.");
                }

                // 2. Determine Status
                string status = activityDto.Status;
                if (string.IsNullOrWhiteSpace(status))
                {
                    bool isTask = activityDto.ActivityTypeName.Equals("Task", StringComparison.OrdinalIgnoreCase);
                    bool isFutureTask = isTask && activityDto.DueDate.HasValue && activityDto.DueDate.Value > DateTime.UtcNow;
                    status = isFutureTask ? "Pending" : "Completed";
                }

                // 3. Create Entity
                var activity = new scrm_dev_mvc.Models.Activity
                {
                    OwnerId = activityDto.OwnerId,
                    ActivityTypeId = activityType.Id,
                    Notes = activityDto.Notes,
                    ActivityDate = activityDto.ActivityDate ?? DateTime.UtcNow,
                    DueDate = activityDto.DueDate,
                    Status = status,
                    ContactId = activityDto.ContactId,
                    DealId = activityDto.DealId,
                    SubjectId = activityDto.SubjectId,
                    SubjectType = activityDto.SubjectType
                };

                // 4. Save using IUnitOfWork
                await _unitOfWork.Activities.AddAsync(activity);
                await _unitOfWork.SaveChangesAsync();

                return activity;
            }

        /// <summary>
        /// Gets all activities directly linked to a specific Contact ID.
        /// Includes related data like ActivityType and Owner.
        /// </summary>
        public async Task<IEnumerable<scrm_dev_mvc.Models.Activity>> GetActivitiesByContactAsync(int contactId)
        {
            _logger.LogInformation("Fetching activities for ContactId: {ContactId}", contactId);

            // 1. Fetch data using the new signature
            var activities = await _unitOfWork.Activities.GetAllAsync(
                predicate: a => a.ContactId == contactId,
                asNoTracking: true,
                includes: new System.Linq.Expressions.Expression<Func<scrm_dev_mvc.Models.Activity, object>>[]
                {
                    a => a.ActivityType,
                    a => a.Owner
                }
            );

            // 2. Apply sorting in-memory
            return activities.OrderByDescending(a => a.ActivityDate);
        }

        /// <summary>
        /// Gets all activities directly linked to a specific Deal ID.
        /// Includes related data like ActivityType and Owner.
        /// </summary>
        public async Task<IEnumerable<scrm_dev_mvc.Models.Activity>> GetActivitiesByDealAsync(int dealId)
        {
            _logger.LogInformation("Fetching activities for DealId: {DealId}", dealId);

            // 1. Fetch data using the new signature
            var activities = await _unitOfWork.Activities.GetAllAsync(
                predicate: a => a.DealId == dealId,
                asNoTracking: true,
                includes: new System.Linq.Expressions.Expression<Func<scrm_dev_mvc.Models.Activity, object>>[]
                {
                    a => a.ActivityType,
                    a => a.Owner,
                    a => a.Contact
                }
            );

            // 2. Apply sorting in-memory
            return activities.OrderByDescending(a => a.ActivityDate);
        }

        /// <summary>
        /// Gets activities by Company, by checking all Contacts and Deals associated with that Company.
        /// Includes related data like ActivityType and Owner.
        /// </summary>
        public async Task<IEnumerable<scrm_dev_mvc.Models.Activity>> GetActivitiesByCompanyAsync(int companyId)
        {
            _logger.LogInformation("Fetching activities for CompanyId: {CompanyId}", companyId);

            // 1. Get all Contact IDs associated with this Company
            // (Assuming GetAllAsync without includes returns a simpler object list)
            var contactIds = (await _unitOfWork.Contacts.GetAllAsync(
                predicate: c => c.CompanyId == companyId,
                asNoTracking: true
            )).Select(c => c.Id).ToList();

            // 2. Get all Deal IDs associated with this Company
            // (Assuming your Deal model has a CompanyId)
            var dealIds = (await _unitOfWork.Deals.GetAllAsync(
                predicate: d => d.CompanyId == companyId,
                asNoTracking: true
            )).Select(d => d.Id).ToList();

            _logger.LogInformation("CompanyId {CompanyId} maps to {ContactCount} contacts and {DealCount} deals.", companyId, contactIds.Count, dealIds.Count);

            // 3. Find activities where ContactId is in the list OR DealId is in the list
            var activities = await _unitOfWork.Activities.GetAllAsync(
                predicate: a => (a.ContactId.HasValue && contactIds.Contains(a.ContactId.Value)) ||
                               (a.DealId.HasValue && dealIds.Contains(a.DealId.Value)),
                asNoTracking: true,
                includes: new System.Linq.Expressions.Expression<Func<scrm_dev_mvc.Models.Activity, object>>[]
                {
                    a => a.ActivityType,
                    a => a.Owner,
                    a => a.Contact,
                    a => a.Deal
                }
            );

            // 4. Apply sorting in-memory
            return activities.OrderByDescending(a => a.ActivityDate);
        }
    }
    
}
