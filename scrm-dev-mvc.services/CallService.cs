using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging; // Add Logger
using scrm_dev_mvc.Data.Repository.IRepository; // Add your UoW namespace
using scrm_dev_mvc.Models; // Add your Models namespace
using scrm_dev_mvc.Models.DTO;
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
        private readonly IUnitOfWork _unitOfWork; // Inject UoW
        private readonly ILogger<CallService> _logger; // Inject Logger
        private readonly IActivityService _activityService;

        // Update constructor to inject IUnitOfWork and ILogger
        public CallService(IConfiguration configuration, IUnitOfWork unitOfWork, ILogger<CallService> logger, IActivityService activityService)
        {
            _accountSid = configuration["Twilio:AccountSid"];
            _authToken = configuration["Twilio:AuthToken"];
            _fromNumber = configuration["Twilio:FromNumber"];
            _unitOfWork = unitOfWork;
            _logger = logger;
            _activityService = activityService;
        }

        // Update signature to accept userId (Guid) and contactId (int)
        public async Task<string> MakeCallAsync(string toPhoneNumber, Guid userId, int contactId)
        {
            try
            {
                TwilioClient.Init(_accountSid, _authToken);

                var call = await CallResource.CreateAsync(
                    url: new Uri("http://demo.twilio.com/docs/voice.xml"), // Using demo XML for now
                    to: new PhoneNumber(toPhoneNumber),
                    from: new PhoneNumber(_fromNumber)
                );

                _logger.LogInformation("Twilio call initiated. SID: {CallSid}", call.Sid);

                // --- SAVE TO DATABASE ---
                // Create a new Call object using your model
                var newCall = new Call
                {
                    Sid = call.Sid,
                    UserId = userId,
                    ContactId = contactId,
                    Direction = "Outbound", // Or determine based on logic
                    Outcome = call.Status.ToString(), // Save initial status (e.g., "queued")
                    CallTime = DateTime.UtcNow,
                    // DurationSeconds will be null until the call is completed
                    // Notes can be added later
                };

                await _unitOfWork.Calls.AddAsync(newCall);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Call record saved to database for User: {UserId}, Contact: {ContactId}, SID: {CallSid}", userId, contactId, call.Sid);
                // --- END SAVE ---
                // --- LOG THE ACTIVITY ---
                var activityDto = new CreateActivityDto
                {
                    OwnerId = userId,
                    ActivityTypeName = "Call",
                    ContactId = contactId,
                    SubjectId = newCall.Id, // Link to the Call record
                    SubjectType = "Call",
                    Notes = $"Called {toPhoneNumber}. Status: {newCall.Outcome}",
                    ActivityDate = newCall.CallTime,
                    Status = "Completed" // Or "Pending" if status is "queued"
                };
                await _activityService.CreateActivityAsync(activityDto); // <-- CALL SERVICE
                _logger.LogInformation("Call activity logged for Contact: {ContactId}", contactId);
                // --- END LOG ---
                return call.Sid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error making Twilio call or saving record for user {UserId} to {ToPhoneNumber}", userId, toPhoneNumber);
                // Re-throw or handle as appropriate for your application
                throw;
            }
        }
    }
}

