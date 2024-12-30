﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MrGutter.Domain.Models;
using MrGutter.Domain;
using MrGutter.Services.IServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MrGutter.Utility;
using Newtonsoft.Json;
using MrGutter.Domain.Models.RequestModel;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace MrGutter.Services.Services
{
    public class UserManager : IUserManager
    {
        public IConfiguration _configuration;
        ILogger _logger;
        IMiscDataSetting _miscDataSetting;
        IUMSService _UMSEmailService;
        EncDcService encDcService = new EncDcService();
        public UserManager(IConfiguration configuration, IMiscDataSetting miscDataSetting, IUMSService UMSEmailService)
        {
            _configuration = configuration;
            _miscDataSetting = miscDataSetting;
            _UMSEmailService = UMSEmailService;
        }

        public async Task<ResponseModel> GetGroupMaster(string? encReq)
        {
            ResponseModel response = new ResponseModel();
            string connStr = UMSResources.GetConnectionString();
            try
            {
                string groupId = await encDcService.Decrypt(encReq);
                string flag = groupId == null || groupId == "" ? "G" : "I";

                groupId = groupId == null || groupId == "" ? "0" : groupId;

                DataSet ds = new DataSet();
                ArrayList arrList = new ArrayList();
                SP.spArgumentsCollection(arrList, "@Flag", flag, "CHAR", "I");
                SP.spArgumentsCollection(arrList, "@GroupId", groupId ?? "0", "INT", "I");
                SP.spArgumentsCollection(arrList, "@Ret", "", "INT", "O");
                SP.spArgumentsCollection(arrList, "@ErrorMsg", "", "VARCHAR", "O");

                ds = SP.RunStoredProcedure(connStr, ds, "sp_GetSetDeleteGroup", arrList);
                if (ds.Tables[0].Rows.Count > 0 && !string.IsNullOrWhiteSpace(ds.Tables[0].Rows[0]["GroupName"]?.ToString()))
                {
                    ds.Tables[0].TableName = "Groups";
                    response.code = 200;
                    response.data = _miscDataSetting.ConvertToJSON(ds.Tables[0]);
                    response.msg = "Success";
                }
                else
                {
                    response.code = -2;
                    response.msg = "No data found.";
                }
            }
            catch (Exception ex)
            {
                response.code = -1;
                response.msg = ex.Message;
            }

            return response;
        }
        public async Task<ResponseModel> CreateOrSetGroupMaster(RequestModel encReq, char flag)
        {
            ResponseModel response = new ResponseModel();
            string connStr = UMSResources.GetConnectionString();
            try
            {
                string json = await encDcService.Decrypt(encReq.V);
                GroupMasterReqModel rq = JsonConvert.DeserializeObject<GroupMasterReqModel>(json);

                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    await connection.OpenAsync();
                    using (SqlCommand command = new SqlCommand("sp_GetSetDeleteGroup", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Add the parameters required by the stored procedure
                        command.Parameters.Add(new SqlParameter("@Flag", SqlDbType.Char)
                        {
                            Value = flag
                        });

                        command.Parameters.Add(new SqlParameter("@GroupId", SqlDbType.Int)
                        {
                            Value = (object)rq.GroupId ?? DBNull.Value
                        });

                        command.Parameters.Add(new SqlParameter("@GroupName", SqlDbType.NVarChar, 100)
                        {
                            Value = (object)rq.GroupName ?? DBNull.Value
                        });

                        command.Parameters.Add(new SqlParameter("@Description", SqlDbType.NVarChar, 255)
                        {
                            Value = (object)rq.Description ?? DBNull.Value
                        });

                        // Output parameters
                        SqlParameter retParam = new SqlParameter("@ret", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(retParam);

                        SqlParameter errorMsgParam = new SqlParameter("@errorMsg", SqlDbType.NVarChar, 200)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(errorMsgParam);

                        // Execute the stored procedure
                        await command.ExecuteNonQueryAsync();

                        // Retrieve output parameters
                        response.code = Convert.ToInt32(retParam.Value);
                        response.msg = Convert.ToString(errorMsgParam.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                response.code = -1;
                response.msg = ex.Message;
            }
            return response;
        }
        public async Task<ResponseModel> GetRoleMaster(string? encReq)
        {
            ResponseModel response = new ResponseModel();
            //string flag = roleId.HasValue ? "I" : "G";
            string roleId = await encDcService.Decrypt(encReq);
            string flag = roleId == null || roleId == "" ? "G" : "I";
            try
            {
                DataSet ds = new DataSet();
                string connStr = UMSResources.GetConnectionString();
                ArrayList arrList = new ArrayList();
                SP.spArgumentsCollection(arrList, "@Flag", flag, "CHAR", "I");
                SP.spArgumentsCollection(arrList, "@RoleId", roleId ?? "0", "INT", "I");
                SP.spArgumentsCollection(arrList, "@Ret", "", "INT", "O");
                SP.spArgumentsCollection(arrList, "@ErrorMsg", "", "VARCHAR", "O");
                ds = SP.RunStoredProcedure(connStr, ds, "sp_GetSetDeleteRole", arrList);
                if (ds.Tables[0].Rows.Count > 0 && !string.IsNullOrWhiteSpace(ds.Tables[0].Rows[0]["RoleName"]?.ToString()))
                {
                    ds.Tables[0].TableName = "Roles";
                    response.code = 200;
                    response.data = _miscDataSetting.ConvertToJSON(ds.Tables[0]);
                    response.msg = "Success";
                }
                else
                {
                    response.code = -2;
                    response.msg = "No data found.";
                }
            }
            catch (Exception ex)
            {
                response.code = -3;
                response.msg = ex.Message;
                // _logger.LogError("GetGroup", ex);
            }
            return await Task.FromResult(response);
        }
        public async Task<ResponseModel> GetRoleByUserId(string encReq)
        {
            ResponseModel response = new ResponseModel();
            //string flag = roleId.HasValue ? "I" : "G";
            string userId = await encDcService.Decrypt(encReq);
            string flag = userId == null || userId == "" ? "G" : "I";
            try
            {
                DataSet ds = new DataSet();
                string connStr = UMSResources.GetConnectionString();
                ArrayList arrList = new ArrayList();
                SP.spArgumentsCollection(arrList, "@Flag", flag, "CHAR", "I");
                SP.spArgumentsCollection(arrList, "@UserId", userId ?? "0", "INT", "I");
                SP.spArgumentsCollection(arrList, "@Ret", "", "INT", "O");
                SP.spArgumentsCollection(arrList, "@ErrorMsg", "", "VARCHAR", "O");
                ds = SP.RunStoredProcedure(connStr, ds, "sp_GetSetDeleteRole", arrList);
                if (ds.Tables[0].Rows.Count > 0 && !string.IsNullOrWhiteSpace(ds.Tables[0].Rows[0]["RoleName"]?.ToString()))
                {
                    ds.Tables[0].TableName = "Roles";
                    response.code = 200;
                    response.data = _miscDataSetting.ConvertToJSON(ds.Tables[0]);
                    response.msg = "Success";
                }
                else
                {
                    response.code = -2;
                    response.msg = "No data found.";
                }
            }
            catch (Exception ex)
            {
                response.code = -3;
                response.msg = ex.Message;
                // _logger.LogError("GetGroup", ex);
            }

            return await Task.FromResult(response);
        }
        public async Task<ResponseModel> CreateOrSetRoleMaster(RequestModel encReq, char flag)
        {
            ResponseModel response = new ResponseModel();
            string connStr = UMSResources.GetConnectionString();
            try
            {
                string json = await encDcService.Decrypt(encReq.V);
                RoleMasterReqModel rq = JsonConvert.DeserializeObject<RoleMasterReqModel>(json);

                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    await connection.OpenAsync();
                    using (SqlCommand command = new SqlCommand("sp_GetSetDeleteRole", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Add the parameters required by the stored procedure
                        command.Parameters.Add(new SqlParameter("@Flag", SqlDbType.Char)
                        {
                            Value = flag
                        });

                        command.Parameters.Add(new SqlParameter("@RoleId", SqlDbType.Int)
                        {
                            Value = (object)rq.RoleId ?? DBNull.Value
                        });

                        command.Parameters.Add(new SqlParameter("@RoleName", SqlDbType.NVarChar, 100)
                        {
                            Value = (object)rq.RoleName ?? DBNull.Value
                        });

                        command.Parameters.Add(new SqlParameter("@Description", SqlDbType.NVarChar, 255)
                        {
                            Value = (object)rq.Description ?? DBNull.Value
                        });

                        // Output parameters
                        SqlParameter retParam = new SqlParameter("@ret", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(retParam);

                        SqlParameter errorMsgParam = new SqlParameter("@errorMsg", SqlDbType.NVarChar, 200)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(errorMsgParam);

                        // Execute the stored procedure
                        await command.ExecuteNonQueryAsync();

                        // Retrieve output parameters
                        response.code = Convert.ToInt32(retParam.Value);
                        response.msg = Convert.ToString(errorMsgParam.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                response.code = -1;
                response.msg = ex.Message;
            }
            return response;
        }
        public async Task<ResponseModel> GetUsers(string? encReq)
        {
            ResponseModel response = new ResponseModel();
            string userId = await encDcService.Decrypt(encReq);
            string flag = userId == null || userId == "" ? "G" : "I";
            try
            {
                DataSet ds = new DataSet();
                string connStr = UMSResources.GetConnectionString();
                ArrayList arrList = new ArrayList();
                SP.spArgumentsCollection(arrList, "@Flag", flag, "CHAR", "I");
                SP.spArgumentsCollection(arrList, "@userId", userId == "" ? "0" : userId, "INT", "I");
                SP.spArgumentsCollection(arrList, "@Ret", "", "INT", "O");
                SP.spArgumentsCollection(arrList, "@ErrorMsg", "", "VARCHAR", "O");
                ds = SP.RunStoredProcedure(connStr, ds, "sp_GetSetDeleteUsers", arrList);

                if (ds.Tables[0].Rows.Count > 0 && !string.IsNullOrWhiteSpace(ds.Tables[0].Rows[0]["UserID"]?.ToString()))
                {
                    ds.Tables[0].TableName = "Users";
                    response.code = 200;
                    response.data = _miscDataSetting.ConvertToJSON(ds.Tables[0]);
                    response.msg = "Success";
                }
                else
                {
                    response.code = -2;
                    response.msg = "No data found.";
                }
            }
            catch (Exception ex)
            {
                response.code = -3;
                response.msg = ex.Message;
                // _logger.LogError("GetGroup", ex);
            }

            return await Task.FromResult(response);
        }
        public async Task<ResponseModel> CreateOrSetUser(RequestModel req, char flag)
        {
            ResponseModel response = new ResponseModel();
            string connStr = UMSResources.GetConnectionString();
            try
            {
                string json = await encDcService.Decrypt(req.V);
                UserMasterReqModel rq = JsonConvert.DeserializeObject<UserMasterReqModel>(json);
                rq.Password = await encDcService.Encrypt(rq.Password);

                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    connection.Open();
                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "sp_GetSetDeleteUsers";
                        command.CommandType = CommandType.StoredProcedure;
                        SqlParameter parameter = new SqlParameter();

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@flag", SqlDbType.Char);
                        parameter.Value = flag;
                        parameter.Direction = ParameterDirection.Input;

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@userId", SqlDbType.Int);
                        parameter.Value = rq.UserId;
                        parameter.Direction = ParameterDirection.Input;

                        //parameter = new SqlParameter();
                        //parameter = command.Parameters.Add("@groupId", SqlDbType.TinyInt);
                        //parameter.Value = rq.GroupId;
                        //parameter.Direction = ParameterDirection.Input;

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@roleId", SqlDbType.TinyInt);
                        parameter.Value = rq.RoleId;
                        parameter.Direction = ParameterDirection.Input;

                        //parameter = new SqlParameter();
                        //parameter = command.Parameters.Add("@userName", SqlDbType.VarChar);
                        //parameter.Value = rq.UserName;
                        //parameter.Direction = ParameterDirection.Input;

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@firstName", SqlDbType.VarChar);
                        parameter.Value = rq.FirstName;
                        parameter.Direction = ParameterDirection.Input;

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@lastName", SqlDbType.VarChar);
                        parameter.Value = rq.LastName;
                        parameter.Direction = ParameterDirection.Input;

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@mobile", SqlDbType.VarChar);
                        parameter.Value = rq.MobileNo;
                        parameter.Direction = ParameterDirection.Input;

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@email", SqlDbType.VarChar);
                        parameter.Value = rq.EmailID;
                        parameter.Direction = ParameterDirection.Input;

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@dob", SqlDbType.SmallDateTime);
                        parameter.Value = rq.DOB;
                        parameter.Direction = ParameterDirection.Input;

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@password", SqlDbType.NVarChar);
                        parameter.Value = rq.Password;
                        parameter.Direction = ParameterDirection.Input;

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@address1", SqlDbType.VarChar);
                        parameter.Value = rq.Address1;
                        parameter.Direction = ParameterDirection.Input;

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@address2", SqlDbType.VarChar);
                        parameter.Value = rq.Address2;
                        parameter.Direction = ParameterDirection.Input;

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@city", SqlDbType.VarChar);
                        parameter.Value = rq.City;
                        parameter.Direction = ParameterDirection.Input;

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@state", SqlDbType.VarChar);
                        parameter.Value = rq.State;
                        parameter.Direction = ParameterDirection.Input;

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@pin", SqlDbType.VarChar);
                        parameter.Value = rq.PinCode;
                        parameter.Direction = ParameterDirection.Input;

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@pan", SqlDbType.VarChar);
                        parameter.Value = rq.PanCardNo;
                        parameter.Direction = ParameterDirection.Input;

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@filePath", SqlDbType.VarChar);
                        parameter.Value = rq.FilePath;
                        parameter.Direction = ParameterDirection.Input;

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@isActive", SqlDbType.Bit);
                        parameter.Value = rq.IsActive;
                        parameter.Direction = ParameterDirection.Input;

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@createdBy", SqlDbType.Int);
                        parameter.Value = rq.CreatedBy;
                        parameter.Direction = ParameterDirection.Input;

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@ret", SqlDbType.Int);
                        parameter.Direction = ParameterDirection.Output;

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@errorMsg", SqlDbType.VarChar, 200);
                        parameter.Direction = ParameterDirection.Output;

                        int res = await command.ExecuteNonQueryAsync();
                        response.code = Convert.ToInt32(command.Parameters["@ret"].Value);
                        response.msg = command.Parameters["@errorMsg"].Value.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                response.code = -1;
                response.data = ex.Message;
                // _logger.LogError("SendForgotPasswordEmail", ex);
            }
            return response;
        }
        public async Task<ResponseModel> DeleteUserMaster(RequestModel req, char flag)
        {
            ResponseModel response = new ResponseModel();
            string connStr = UMSResources.GetConnectionString();
            try
            {
                string userId = await encDcService.Decrypt(req.V);
                UserMasterReqModel rq = new UserMasterReqModel();
                //UserMasterReqModel rq = JsonConvert.DeserializeObject<UserMasterReqModel>(json);
                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    connection.Open();
                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "sp_GetSetDeleteUsers";
                        command.CommandType = CommandType.StoredProcedure;
                        SqlParameter parameter = new SqlParameter();

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@flag", SqlDbType.Char);
                        parameter.Value = flag;
                        parameter.Direction = ParameterDirection.Input;

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@userId", SqlDbType.Int);
                        parameter.Value = userId;
                        parameter.Direction = ParameterDirection.Input;

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@createdBy", SqlDbType.Int);
                        parameter.Value = rq.CreatedBy;
                        parameter.Direction = ParameterDirection.Input;

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@ret", SqlDbType.Int);
                        parameter.Direction = ParameterDirection.Output;

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@errorMsg", SqlDbType.VarChar, 200);
                        parameter.Direction = ParameterDirection.Output;

                        int res = await command.ExecuteNonQueryAsync();
                        response.code = Convert.ToInt32(command.Parameters["@ret"].Value);
                        response.msg = command.Parameters["@errorMsg"].Value.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                response.code = -1;
                response.data = ex.Message;
                // _logger.LogError("SendForgotPasswordEmail", ex);
            }
            return response;
        }
        public async Task<ResponseModel> CreateLogHistory(RequestModel req)
        {
            ResponseModel response = new ResponseModel();
            string connStr = UMSResources.GetConnectionString();
            try
            {
                string json = await encDcService.Decrypt(req.V);
                LogHistoryReqModel rq = JsonConvert.DeserializeObject<LogHistoryReqModel>(json);


                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    connection.Open();
                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "sp_GetSetLogHistory";
                        command.CommandType = CommandType.StoredProcedure;
                        SqlParameter parameter = new SqlParameter();

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@flag", SqlDbType.Char);
                        parameter.Value = 'G';
                        parameter.Direction = ParameterDirection.Input;

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@userId", SqlDbType.Int);
                        parameter.Value = rq.UserId;
                        parameter.Direction = ParameterDirection.Input;

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@actions", SqlDbType.TinyInt);
                        parameter.Value = rq.Actions;
                        parameter.Direction = ParameterDirection.Input;

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@details", SqlDbType.VarChar);
                        parameter.Value = rq.Details;
                        parameter.Direction = ParameterDirection.Input;

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@ret", SqlDbType.Int);
                        parameter.Direction = ParameterDirection.Output;

                        parameter = new SqlParameter();
                        parameter = command.Parameters.Add("@errorMsg", SqlDbType.VarChar, 200);
                        parameter.Direction = ParameterDirection.Output;

                        int res = await command.ExecuteNonQueryAsync();
                        response.code = Convert.ToInt32(command.Parameters["@ret"].Value);
                        response.msg = command.Parameters["@errorMsg"].Value.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                response.code = -1;
                response.data = ex.Message;
                // _logger.LogError("SendForgotPasswordEmail", ex);
            }
            return response;
        }
        public async Task<ResponseModel> GetLogHistory()
        {
            ResponseModel response = new ResponseModel();
            try
            {
                DataSet ds = new DataSet();
                string connStr = UMSResources.GetConnectionString();
                ArrayList arrList = new ArrayList();
                SP.spArgumentsCollection(arrList, "@Flag", "G", "CHAR", "I");
                SP.spArgumentsCollection(arrList, "@Ret", "", "INT", "O");
                SP.spArgumentsCollection(arrList, "@ErrorMsg", "", "VARCHAR", "O");
                ds = SP.RunStoredProcedure(connStr, ds, "sp_GetSetLogHistory", arrList);

                if (ds.Tables[0].Rows.Count > 0 && !string.IsNullOrWhiteSpace(ds.Tables[0].Rows[0]["LogId"]?.ToString()))
                {
                    ds.Tables[0].TableName = "LogHistory";
                    response.code = 200;
                    response.data = _miscDataSetting.ConvertToJSON(ds.Tables[0]);
                    response.msg = "Success";
                }
                else
                {
                    response.code = -2;
                    response.msg = "No data found.";
                }
            }
            catch (Exception ex)
            {
                response.code = -3;
                response.msg = ex.Message;
                // _logger.LogError("GetGroup", ex);
            }

            return await Task.FromResult(response);
        }
    }
}