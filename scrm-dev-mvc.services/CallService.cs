using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging; 
using scrm_dev_mvc.Data.Repository.IRepository; 
using scrm_dev_mvc.Models; 
using scrm_dev_mvc.Models.DTO;
using scrm_dev_mvc.services.Interfaces;
using System;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace scrm_dev_mvc.Services
{
    public class CallService : ICallService
    {
        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _fromNumber;
        private readonly IUnitOfWork _unitOfWork; 
        private readonly ILogger<CallService> _logger; 
        private readonly IActivityService _activityService;

        public CallService(IConfiguration configuration, IUnitOfWork unitOfWork, ILogger<CallService> logger, IActivityService activityService)
        {
            _accountSid = configuration["Twilio:AccountSid"];
            _authToken = configuration["Twilio:AuthToken"];
            _fromNumber = configuration["Twilio:FromNumber"];
            _unitOfWork = unitOfWork;
            _logger = logger;
            _activityService = activityService;
        }

        public async Task<string> MakeCallAsync(string toPhoneNumber, Guid userId, int contactId)
        {
            try
            {
                TwilioClient.Init(_accountSid, _authToken);

                var call = await CallResource.CreateAsync(
                    url: new Uri("http://demo.twilio.com/docs/voice.xml"), 
                    to: new PhoneNumber(toPhoneNumber),
                    from: new PhoneNumber(_fromNumber)
                );

                _logger.LogInformation("Twilio call initiated. SID: {CallSid}", call.Sid);

                
                var newCall = new Call
                {
                    Sid = call.Sid,
                    UserId = userId,
                    ContactId = contactId,
                    Direction = "Outbound", 
                    Outcome = call.Status.ToString(), 
                    CallTime = DateTime.UtcNow,
                    
                };

                await _unitOfWork.Calls.AddAsync(newCall);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Call record saved to database for User: {UserId}, Contact: {ContactId}, SID: {CallSid}", userId, contactId, call.Sid);
                
                var activityDto = new CreateActivityDto
                {
                    OwnerId = userId,
                    ActivityTypeName = "Call",
                    ContactId = contactId,
                    SubjectId = newCall.Id, 
                    SubjectType = "Call",
                    Notes = $"Called {toPhoneNumber}. Status: {newCall.Outcome}",
                    ActivityDate = newCall.CallTime,
                    Status = "Completed" 
                };
                await _activityService.CreateActivityAsync(activityDto); 
                _logger.LogInformation("Call activity logged for Contact: {ContactId}", contactId);
                return call.Sid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error making Twilio call or saving record for user {UserId} to {ToPhoneNumber}", userId, toPhoneNumber);
                throw;
            }
        }
    }
}

