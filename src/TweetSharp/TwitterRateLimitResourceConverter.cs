using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TweetSharp
{
    // [DC]: All converters must be public for Silverlight to construct them correctly.
    /*
     *  {
	        "rate_limit_context": {
		        "access_token": "2544079980-NRaZMwOeTiV9RrfHBIkYCe7Xo3R10QOBa48ho2h"
	        },
	        "resources": {
		        "followers": {
		            "\/followers\/list": {
				        "limit": 15,
				        "remaining": 15,
				        "reset": 1404396957
			        },
			        "\/followers\/ids": { 
				        "limit": 15,
				        "remaining":15,
				        "reset":1404396957
			        }
		        },
                "account" : {
                    "\/account\/verify_credentials": {
                        "limit": 15,
				        "remaining": 15,
				        "reset": 1404396957
			        },
                    "\/account\/settings":  {
                        "limit": 15,
				        "remaining": 15,
				        "reset": 1404396957
			        },
                }
	        },
        }
     */
    public class TwitterRateLimitResourceConverter : TwitterConverterBase
    {
        public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            var result = new TwitterRateLimitStatusSummary();

            var rootObject = JObject.Load(reader);
            result.RawSource = rootObject.ToString();

            result.AccessToken = GetAccessToken(rootObject);

            result.Resources = GetResources(rootObject);

            return result;
        }

        public override bool CanConvert(Type objectType)
        {
            var t = (IsNullableType(objectType)) ? Nullable.GetUnderlyingType(objectType) : objectType;

            return typeof(TwitterRateLimitStatusSummary).IsAssignableFrom(t);
        }

        private string GetAccessToken(JObject rootObject)
        {
            JObject context = JObject.Load(rootObject.GetValue("rate_limit_context").CreateReader());
            return context.GetValue("access_token").Value<string>();
        }

        private List<TwitterRateLimitResource> GetResources(JObject rootObject)
        {
            JObject resourcesBuckets = JObject.Load(rootObject.GetValue("resources").CreateReader()); // resources
            if (resourcesBuckets == null || resourcesBuckets.Count <= 0)
                return null;

            var resources = new List<TwitterRateLimitResource>();
            foreach (var resourceBucket in resourcesBuckets)                            // followers, account
            {
                var newBucket = new TwitterRateLimitResource();
                resources.Add(newBucket);

                newBucket.Name = resourceBucket.Key;                                               

                var rawBucketLimits = resourceBucket.Value;                             // /followers/list, /followers/ids or /account/verify_credentials, /account_settings
                if (rawBucketLimits == null) continue;

                var bucketLimits = JObject.Load(rawBucketLimits.CreateReader());        
                newBucket.Limits = GetBucketLimits(bucketLimits);
            }

            return resources;
        }

        private Dictionary<string, TwitterRateLimitStatus> GetBucketLimits(JObject limits)          
        {
            if (limits == null || limits.Count <= 0)
                return null;

            var bucketLimits = new Dictionary<string, TwitterRateLimitStatus>();

            foreach (var limit in limits)                                           // /followers/list, /followers/ids or /account/verify_credentials, /account_settings
            {
                var bucketLimit = new TwitterRateLimitStatus();                     // limit, remaining, reset
                bucketLimits.Add(limit.Key, bucketLimit);

                JObject limitValues = JObject.Load(limit.Value.CreateReader());
                bucketLimit.RawSource = limitValues.ToString();

                bucketLimit.HourlyLimit = limitValues.GetValue("limit").Value<int>();
                bucketLimit.RemainingHits = limitValues.GetValue("remaining").Value<int>();
                bucketLimit.ResetTime = limitValues.GetValue("reset").Value<long>().FromUnixTime();

                var remainingSeconds = Convert.ToInt64((bucketLimit.ResetTime - DateTime.UtcNow).TotalSeconds);
                bucketLimit.ResetTimeInSeconds = remainingSeconds < 0 ? -1 : remainingSeconds;
            }

            return bucketLimits;
        }
    }
}
