using System;
using NetEscapades.Configuration.Validation;

namespace SumoLogicHangfire.Configurations
{
    public class SumoLogicSettings : IValidatable
    {
        public Uri BaseUri { get; set; }
        public string AccessId { get; set; }
        public string AccessKey { get; set; }

        public void Validate()
        {
            if (BaseUri is null || !Uri.IsWellFormedUriString(BaseUri.AbsoluteUri, UriKind.Absolute))
                throw new Exception($"{nameof(SumoLogicSettings)}.{nameof(BaseUri)} is not a valid URI");
            if (string.IsNullOrEmpty(AccessId))
                throw new Exception($"{nameof(SumoLogicSettings)}.{nameof(AccessId)} must not be null or empty");
            if (string.IsNullOrEmpty(AccessKey))
                throw new Exception($"{nameof(SumoLogicSettings)}.{nameof(AccessKey)} must not be null or empty");
        }
    }
}
