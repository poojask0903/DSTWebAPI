using DSTUpdateTimeZoneDataBaseWebAPI.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace DSTUpdateTimeZoneDataBaseWebAPI.Controllers
{
    public class CallTimeAndDateExtAPIController : ApiController
    {
        //
        // GET: /CallTimeAndDateExtAPI/

        // GET api/values
        // API Execution will begin from here
        public async Task<IEnumerable<string>> Get()
        {
            var result = await GetExternalResponse();
            return new string[] { result, "Pass" };
        }

        // This function will call the external API
        private async Task<string> GetExternalResponse()
        {
            var client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(Constants.EXTERNAL_TIME_AND_DATE_API_URL);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            ParseJSONData(result);
            return result;
        }

        // This will parse the JSON data returned by external API
        private static void ParseJSONData(string result)
        {
            // Parse JSON
            dynamic dynObj = JObject.Parse(result);
            List<DSTAPIProperties> lstDSTProperties = new List<DSTAPIProperties>();
            foreach (var data in dynObj.dstlist)
            {
                string timeZoneNameTemp = string.Empty;
                string timeZoneOffsetTemp = string.Empty;
                string dstTimeZoneNameTemp = string.Empty;
                DateTime dstStartDateTemp = new DateTime();
                DateTime dstEndDateTemp = new DateTime();
                string dstOffsetTemp = string.Empty;
                string biggestPlaceTemp = string.Empty;
                string countryTemp = string.Empty;
                string descTemp = string.Empty;

                if (data.region.desc != null)
                {
                    descTemp = Convert.ToString(data.region.desc.Value);
                }

                if (data.region.country.name != null)
                {
                    countryTemp = Convert.ToString(data.region.country.name.Value);
                }

                if (data.region.biggestplace != null)
                {
                    biggestPlaceTemp = Convert.ToString(data.region.biggestplace.Value);
                }

                if (data.stdtimezone.zonename != null)
                {
                    timeZoneNameTemp = Convert.ToString(data.stdtimezone.zonename.Value);
                }
                if (data.stdtimezone.offset != null)
                {
                    timeZoneOffsetTemp = Convert.ToString(data.stdtimezone.offset.Value);
                }
                if (data.dsttimezone != null)
                {
                    if (data.dsttimezone.zonename != null)
                    {
                        dstTimeZoneNameTemp = Convert.ToString(data.dsttimezone.zonename.Value);
                    }
                    if (data.dststart != null)
                    {
                        dstStartDateTemp = Convert.ToDateTime(data.dststart.Value);
                    }
                    if (data.dstend != null)
                    {
                        dstEndDateTemp = Convert.ToDateTime(data.dstend.Value);
                    }
                    if (data.dsttimezone != null)
                    {
                        dstOffsetTemp = Convert.ToString(data.dsttimezone.offset.Value);
                    }
                }
                else
                {
                    dstTimeZoneNameTemp = "";
                    dstOffsetTemp = "";
                }

                lstDSTProperties.Add(new DSTAPIProperties(timeZoneNameTemp, "", timeZoneOffsetTemp, dstTimeZoneNameTemp, dstStartDateTemp, dstEndDateTemp, 0, dstOffsetTemp, biggestPlaceTemp, countryTemp, descTemp));
            }

            // If data is rceived from the external API, then call below functions for further processing on Time Zone database
            if (0 < lstDSTProperties.Count)
            {
                UpdateTimeZoneDB(lstDSTProperties);
                UpdateIsCurretTimeDST();
            }
        }

        // Based on the DST Start Time and End Time, It will determine whether current time is DST or not
        private static void UpdateIsCurretTimeDST()
        {
            using (SqlConnection connection = new SqlConnection(ConfigurationManager.AppSettings["DefaultConnection"].ToString()))
            {
                // Check Connection Open or Close
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = Constants.GET_ALL_ROW_TIMEZOME;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!DBNull.Value.Equals(reader[Constants.DST_START_DATE_DB_COLUMN]) && !DBNull.Value.Equals(reader[Constants.DST_END_DATE_DB_COLUMN]))
                            {
                                //not null
                                DateTime dststartDate = DateTime.Parse(Convert.ToString(reader[Constants.DST_START_DATE_DB_COLUMN]));
                                DateTime dstEndDate = DateTime.Parse(Convert.ToString(reader[Constants.DST_END_DATE_DB_COLUMN]));
                                DateTime now = DateTime.Now;
                                if ((now > dststartDate) && (now < dstEndDate))
                                {
                                    //match found
                                    string queryToUpdate = "Update [dbo].[TimeZoneDetails] set [IsCurrentTimeDST] = '" + 1 + "' where [Ref_TimeZone_Id] = '" + reader["Ref_TimeZone_Id"].ToString() + "'";
                                    using (SqlCommand commandUpdateIsCurrentTimeDST = new SqlCommand(queryToUpdate, connection))
                                    {
                                        commandUpdateIsCurrentTimeDST.ExecuteNonQuery();
                                    }
                                }
                                else
                                {
                                    string queryToUpdate = "Update [dbo].[TimeZoneDetails] set [IsCurrentTimeDST] = '" + 0 + "' where [Ref_TimeZone_Id] = '" + reader["Ref_TimeZone_Id"].ToString() + "'";
                                    using (SqlCommand commandUpdateIsCurrentTimeDST = new SqlCommand(queryToUpdate, connection))
                                    {
                                        commandUpdateIsCurrentTimeDST.ExecuteNonQuery();
                                    }
                                }
                            }
                            else
                            {
                                string query = "Update [dbo].[TimeZoneDetails] set [IsCurrentTimeDST] = '" + 0 + "' where [Ref_TimeZone_Id] = '" + reader["Ref_TimeZone_Id"].ToString() + "'";
                                using (SqlCommand commandUpdateIsCurrentTimeDST = new SqlCommand(query, connection))
                                {
                                    commandUpdateIsCurrentTimeDST.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
            }
        }

        // Update Time Zone Database based on the data fetched from the external API
        private static void UpdateTimeZoneDB(List<DSTAPIProperties> lstDSTProperties)
        {
            using (SqlConnection connection = new SqlConnection(Convert.ToString(ConfigurationManager.AppSettings["DefaultConnection"])))
            {
                // Check Connection Open or Close
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = Constants.GET_ALL_ROW_TIMEZOME;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var value = Convert.ToString(reader[Constants.TIMEZONE_NAME_DB_COLUMN]);
                            DSTAPIProperties reqdListItem = lstDSTProperties.Find(x => (x.TimeZoneName.ToLower() == value.ToLower()));

                            var stateOrCountry = Convert.ToString(reader["Time"]);
                            int posA = stateOrCountry.LastIndexOf("0)");
                            DSTAPIProperties reqdListItemStateCountryBasis = null;
                            DSTAPIProperties reqdListItemCountry = null;
                            //DSTAPIProperties reqdListItemDesc = null;
                            if (posA != -1)
                            {
                                int adjustedPosA = posA + "0)".Length;
                                string toFind = stateOrCountry.Substring(adjustedPosA);
                                string[] words = toFind.Split(',');

                                foreach (string word in words)
                                {
                                    reqdListItemStateCountryBasis = lstDSTProperties.Find(x => (x.BiggestPlace.ToLower() == word.Trim().ToLower()));
                                    reqdListItemCountry = lstDSTProperties.Find(x => (x.Country.ToLower() == word.Trim().ToLower()));
                                    // reqdListItemDesc = lstDSTProperties.Find(x => (x.Desc.ToLower().Contains(word.Trim().ToLower())));
                                    if (reqdListItemStateCountryBasis != null || reqdListItemCountry != null)
                                    {
                                        break;
                                    }
                                }

                            }

                            if (posA == -1)
                            {
                                int posB = stateOrCountry.LastIndexOf("(GMT)");
                                if (posB != -1)
                                {
                                    int adjustedPosB = posB + "(GMT)".Length;
                                    string toFind = stateOrCountry.Substring(adjustedPosB);
                                    string[] words = toFind.Split(',');

                                    foreach (string word in words)
                                    {
                                        reqdListItemStateCountryBasis = lstDSTProperties.Find(x => (x.BiggestPlace.ToLower() == word.Trim().ToLower()));
                                        reqdListItemCountry = lstDSTProperties.Find(x => (x.Country.ToLower() == word.Trim().ToLower()));
                                        // reqdListItemDesc = lstDSTProperties.Find(x => (x.Desc.ToLower().Contains(word.Trim().ToLower())));
                                        if (reqdListItemStateCountryBasis != null || reqdListItemCountry != null)
                                        {
                                            break;
                                        }
                                    }
                                }
                            }


                            if (reqdListItem != null)
                            {
                                string dstTimeZoneNameInDB = Convert.ToString(reader[Constants.DST_TIMEZONE_NAME_DB_COLUMN]);
                                if (string.IsNullOrEmpty(dstTimeZoneNameInDB))
                                {
                                    using (SqlCommand commandDSTTimeZoneName = connection.CreateCommand())
                                    {
                                        // Set the column in DB
                                        commandDSTTimeZoneName.CommandText = "Update [dbo].[TimeZoneDetails] set [DSTTimeZoneName] = '" + reqdListItem.DSTTimeZoneName + "' where [TimeZoneName] = '" + value + "'";
                                        commandDSTTimeZoneName.ExecuteNonQuery();
                                    }
                                }
                                string dstStartDateInDB = Convert.ToString(reader[Constants.DST_START_DATE_DB_COLUMN]);
                                if (string.IsNullOrEmpty(dstStartDateInDB))
                                {
                                    // Set the column in DB
                                    using (SqlCommand commandDSTStartDate = connection.CreateCommand())
                                    {
                                        // Set the column in DB
                                        commandDSTStartDate.CommandText = "Update [dbo].[TimeZoneDetails] set [DSTStartDate] = '" + reqdListItem.DSTStartDate + "' where [TimeZoneName] = '" + value + "'";
                                        commandDSTStartDate.ExecuteNonQuery();
                                    }
                                }
                                else if (!(dstStartDateInDB.Equals(Convert.ToString(reqdListItem.DSTStartDate))))
                                {
                                    // Update the column as per latest value in list(API)
                                }
                                string dstEndDateInDB = Convert.ToString(reader[Constants.DST_END_DATE_DB_COLUMN]);
                                if (string.IsNullOrEmpty(dstEndDateInDB))
                                {
                                    // Set the column in DB
                                    using (SqlCommand commandDSTEndDate = connection.CreateCommand())
                                    {
                                        // Set the column in DB
                                        commandDSTEndDate.CommandText = "Update [dbo].[TimeZoneDetails] set [DSTEndDate] = '" + reqdListItem.DSTEndDate + "' where [TimeZoneName] = '" + value + "'";
                                        commandDSTEndDate.ExecuteNonQuery();
                                    }
                                }
                                else if (!(dstEndDateInDB.Equals(Convert.ToString(reqdListItem.DSTEndDate))))
                                {
                                    // Update the column as per latest value in list(API)
                                }
                                string dstTimeDifferenceInDB = Convert.ToString(reader[Constants.DST_TIME_DIFFERENCE_DB_COLUMN]);
                                if (string.IsNullOrEmpty(dstTimeDifferenceInDB))
                                {
                                    // Set the column in DB
                                    using (SqlCommand commandDSTTimeDiff = connection.CreateCommand())
                                    {
                                        // Set the column in DB
                                        commandDSTTimeDiff.CommandText = "Update [dbo].[TimeZoneDetails] set [DSTTimeDifference] = '" + reqdListItem.DSTTimeDifference + "' where [TimeZoneName] = '" + value + "'";
                                        commandDSTTimeDiff.ExecuteNonQuery();
                                    }
                                }
                                else if (!(dstTimeDifferenceInDB.Equals(Convert.ToString(reqdListItem.DSTTimeDifference))))
                                {
                                    // Update the column as per latest value in list(API)
                                }
                            }
                        }
                    }
                }

            }
        }

        // Add new entry in DB
        private static void InsertInDB(DSTAPIProperties lstDSTItem, SqlConnection connection)
        {
            // Create a new entry in TimeZone table 
            string query = "INSERT INTO [dbo].[TimeZoneDetails] (TimeZoneName, Time, TimeDifference, DSTTimeZoneName, DSTStartDate,  DSTEndDate, IsCurrentTimeDST, DSTTimeDifference)";
            query += " VALUES (@TimeZoneName, @Time, @TimeDifference, @DSTTimeZoneName, @DSTStartDate,  @DSTEndDate, @IsCurrentTimeDST, @DSTTimeDifference)";
            using (SqlCommand commandAddNewDSTCountry = new SqlCommand())
            {
                commandAddNewDSTCountry.Connection = connection;
                commandAddNewDSTCountry.CommandText = query;
                commandAddNewDSTCountry.Parameters.AddWithValue("@TimeZoneName", lstDSTItem.TimeZoneName);
                commandAddNewDSTCountry.Parameters.AddWithValue("@Time", lstDSTItem.Time);
                commandAddNewDSTCountry.Parameters.AddWithValue("@TimeDifference", lstDSTItem.TimeDifference);
                commandAddNewDSTCountry.Parameters.AddWithValue("@DSTTimeZoneName", lstDSTItem.DSTTimeZoneName);
                commandAddNewDSTCountry.Parameters.AddWithValue("@DSTStartDate", lstDSTItem.DSTStartDate);
                commandAddNewDSTCountry.Parameters.AddWithValue("@DSTEndDate", lstDSTItem.DSTEndDate);
                commandAddNewDSTCountry.Parameters.AddWithValue("@IsCurrentTimeDST", lstDSTItem.IsCurrentTimeDST);
                commandAddNewDSTCountry.Parameters.AddWithValue("@DSTTimeDifference", lstDSTItem.DSTTimeDifference);
                commandAddNewDSTCountry.ExecuteNonQuery();
            }
        }
    }
}

