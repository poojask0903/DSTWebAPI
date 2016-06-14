using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DSTUpdateTimeZoneDataBaseWebAPI.Models
{
    public class DSTAPIProperties
    {
        public string TimeZoneName;
        public string Time;
        public string TimeDifference;
        public string DSTTimeZoneName;
        public DateTime DSTStartDate;
        public DateTime DSTEndDate;
        public int IsCurrentTimeDST;
        public string DSTTimeDifference;
        public string BiggestPlace;
        public string Country;
        public string Desc;

        public DSTAPIProperties(string timeZoneName, string time, string timeDifference, string dstTimeZoneName, DateTime dstStartDate, DateTime dstEndDate, int isCurrentTimeDST, string dstTimeDifference, string biggestPlace, string country , string desc)
        {
            this.TimeZoneName = timeZoneName;
            this.Time = time;
            this.TimeDifference = timeDifference;
            this.DSTTimeZoneName = dstTimeZoneName;
            this.DSTStartDate = dstStartDate;
            this.DSTEndDate = dstEndDate;
            this.IsCurrentTimeDST = isCurrentTimeDST;
            this.DSTTimeDifference = dstTimeDifference;
            this.BiggestPlace = biggestPlace;
            this.Country = country;
            this.Desc = desc;
        }
    }
}