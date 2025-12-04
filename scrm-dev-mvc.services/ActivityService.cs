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

            var activityType = await _unitOfWork.ActivityTypes.FirstOrDefaultAsync(
                    at => at.Name.ToLower() == activityDto.ActivityTypeName.ToLower()
                );

                if (activityType == null)
                {
                    throw new InvalidOperationException($"Activity type '{activityDto.ActivityTypeName}' not found.");
                }

                string status = activityDto.Status;
                if (string.IsNullOrWhiteSpace(status))
                {
                    bool isTask = activityDto.ActivityTypeName.Equals("Task", StringComparison.OrdinalIgnoreCase);
                    bool isFutureTask = isTask && activityDto.DueDate.HasValue && activityDto.DueDate.Value > DateTime.UtcNow;
                    status = isFutureTask ? "Pending" : "Completed";
                }

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

                await _unitOfWork.Activities.AddAsync(activity);
                await _unitOfWork.SaveChangesAsync();

                return activity;
            }

        
        public async Task<IEnumerable<scrm_dev_mvc.Models.Activity>> GetActivitiesByContactAsync(int contactId)
        {
            _logger.LogInformation("Fetching activities for ContactId: {ContactId}", contactId);

            var activities = await _unitOfWork.Activities.GetAllAsync(
                predicate: a => a.ContactId == contactId,
                asNoTracking: true,
                includes: new System.Linq.Expressions.Expression<Func<scrm_dev_mvc.Models.Activity, object>>[]
                {
                    a => a.ActivityType,
                    a => a.Owner
                }
            );

            return activities.OrderByDescending(a => a.ActivityDate);
        }

        
        public async Task<IEnumerable<scrm_dev_mvc.Models.Activity>> GetActivitiesByDealAsync(int dealId)
        {
            _logger.LogInformation("Fetching activities for DealId: {DealId}", dealId);

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

            return activities.OrderByDescending(a => a.ActivityDate);
        }

       
        public async Task<IEnumerable<scrm_dev_mvc.Models.Activity>> GetActivitiesByCompanyAsync(int companyId)
        {
            _logger.LogInformation("Fetching activities for CompanyId: {CompanyId}", companyId);

            
            var contactIds = (await _unitOfWork.Contacts.GetAllAsync(
                predicate: c => c.CompanyId == companyId,
                asNoTracking: true
            )).Select(c => c.Id).ToList();

           
            var dealIds = (await _unitOfWork.Deals.GetAllAsync(
                predicate: d => d.CompanyId == companyId,
                asNoTracking: true
            )).Select(d => d.Id).ToList();

            _logger.LogInformation("CompanyId {CompanyId} maps to {ContactCount} contacts and {DealCount} deals.", companyId, contactIds.Count, dealIds.Count);

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

            return activities.OrderByDescending(a => a.ActivityDate);
        }

        public async Task<bool> DeleteActivityBySubjectAsync(int subjectId, string subjectType)
        {
            if (string.IsNullOrEmpty(subjectType))
            {
                return false;
            }

            try
            {
                
                var activity = await _unitOfWork.Activities.FirstOrDefaultAsync(a =>
                    a.SubjectId == subjectId &&
                    a.SubjectType == subjectType
                );

                if (activity != null)
                {
                    _unitOfWork.Activities.Delete(activity);
                    
                    _logger.LogInformation("Activity {ActivityId} marked for deletion (Subject: {SubjectId}).", activity.Id, subjectId);
                }
                else
                {
                    _logger.LogWarning("No activity found to delete for Subject {SubjectId} of type {SubjectType}.", subjectId, subjectType);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding activity for deletion (Subject: {SubjectId}).", subjectId);
                return false;
            }
        }
    }
    
}
