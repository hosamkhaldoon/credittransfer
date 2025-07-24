using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Virgin.HTTPService
{
    public class Constants
    {
    }

    public enum IDNumberResultStatus
    {
        SUCCESS = 0,
        ID_IS_NOT_MATCHED = 1,
        CIVIL_ID_IS_NOT_MATCHING = 2,
        UNKNOWN_ERROR = 3, 
    }

}