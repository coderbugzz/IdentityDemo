using Dapper;
using IdentityDemo.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityDemo.Repository
{
    public class Repository : IRepository
    {
        private readonly IConfiguration _configuration;

        public Repository(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<Response<IdentityModel>> LoginAsync(LoginViewModel loginView)
        {
            Response<IdentityModel> response = new Response<IdentityModel>();

            var sp_params = new DynamicParameters();
            sp_params.Add("email",loginView.Email,DbType.String);
            sp_params.Add("password", loginView.Password, DbType.String);

            try
            {
                using IDbConnection dbConnection = new SqlConnection(_configuration.GetConnectionString("default"));

                response.Data = await dbConnection.QueryFirstOrDefaultAsync<IdentityModel>("sp_loginUser", sp_params, commandType: CommandType.StoredProcedure);
                response.message = (response.Data is null) ? "Login failed.Please check Username and / or password" : "data found";
                response.code = (response.Data is null) ? 500 : 200;
            }
            catch (Exception ex)
            {
                response.code = 500;
                response.message = ex.Message;
            }
            
           return response;
        }

        public async Task<Response<string>> RegisterAsync(RegisterViewModel registerView)
        {
            Response<string> response = new Response<string>();
            var sp_params = new DynamicParameters();
            sp_params.Add("email", registerView.Email, DbType.String);
            sp_params.Add("password", registerView.Password, DbType.String);
            sp_params.Add("role", registerView.Role, DbType.String);
            sp_params.Add("retVal", DbType.String,direction:ParameterDirection.Output);


            using (IDbConnection dbConnection = new SqlConnection(_configuration.GetConnectionString("default")))
            {
                if (dbConnection.State == ConnectionState.Closed) dbConnection.Open();
                using var transaction = dbConnection.BeginTransaction();
                try
                {
                    await dbConnection.QueryAsync<string>("sp_registerUser", sp_params, commandType: CommandType.StoredProcedure, transaction: transaction);
                    response.code = sp_params.Get<int>("retVal"); //get output parameter value
                    transaction.Commit();
                    response.message = (response.code == 200) ? "Successfully Registered" : "Unable to register user";
                    
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    response.Data = ex.Message;
                    response.message = "An error encountered during saving!";
                    response.code = 500;
                }
            };
            return response;
        }
    }
}
