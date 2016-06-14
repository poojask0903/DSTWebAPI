using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DSTUpdateTimeZoneDataBaseWebAPI.Models
{
    public static class Constants
    {
        public static string EXTERNAL_TIME_AND_DATE_API_URL = "https://api.xmltime.com/dstlist?accesskey=nv35mhwcNv&expires=2016-05-04T13%3A02%3A35%2B00%3A00&signature=%2Fk9Tf1CbCq0MSvz%2FIwtUGKSb6Ak%3D&version=2&out=js&year=2016";
        public static string CONNECTION_STRING_KEY = "DefaultConnection";
        public static string GET_ALL_ROW_TIMEZOME = "SELECT * FROM [dbo].[TimeZoneDetails]";
        public static string DST_START_DATE_DB_COLUMN = "DSTStartDate";
        public static string DST_END_DATE_DB_COLUMN = "DSTEndDate";
        public static string TIMEZONE_TABLE_NAME = "[dbo].[TimeZoneDetails]";
        public static string TIMEZONE_NAME_DB_COLUMN = "TimeZoneName";
        public static string DST_TIMEZONE_NAME_DB_COLUMN = "DSTTimeZoneName";
        public static string DST_TIME_DIFFERENCE_DB_COLUMN = "DSTTimeDifference";
    }
}