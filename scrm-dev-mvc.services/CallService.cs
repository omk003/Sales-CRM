using System;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Microsoft.Extensions.Configuration;

namespace scrm_dev_mvc.Services
{
   
    public class CallService : ICallService
    {
        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _fromNumber;

        public CallService(IConfiguration configuration)
        {
            _accountSid = configuration["Twilio:AccountSid"];
            _authToken = configuration["Twilio:AuthToken"];
            _fromNumber = configuration["Twilio:FromNumber"]; // keep fixed here
        }

        public async Task<string> MakeCallAsync(string toPhoneNumber)
        {
            TwilioClient.Init(_accountSid, _authToken);

            var call = await CallResource.CreateAsync(
                url: new Uri("http://demo.twilio.com/docs/voice.xml"),
                to: new PhoneNumber(toPhoneNumber),
                from: new PhoneNumber(_fromNumber)
            );

            return call.Sid;
        }

        // V2.0
        //public async Task<string> MakeCallAsync(string toPhoneNumber)
        //{
        //    TwilioClient.Init(_accountSid, _authToken);

        //    var call = await CallResource.CreateAsync(
        //        url: new Uri("https://maudlinly-nonreactive-arturo.ngrok-free.dev/api/call/voice"), 
        //        to: new PhoneNumber(toPhoneNumber),                    // receiver’s number
        //        from: new PhoneNumber(_fromNumber)                     // your Twilio number
        //    );

        //    return call.Sid;
        //}

    }

}
