﻿using System;
using System.Collections.Generic;
using System.Linq;
using KeySndr.Base.Providers;
using KeySndr.Common;

namespace KeySndr.Base.Commands
{
    public class GetAllScripts : ICommand<ApiResult<IEnumerable<string>>>
    {
        private readonly IScriptProvider scriptProvider;
        public ApiResult<IEnumerable<string>> Result { get; private set; }
        

        public GetAllScripts(IScriptProvider s)
        {
            scriptProvider = s;
        }

        public void Execute()
        {
            try
            {
                Result = new ApiResult<IEnumerable<string>>
                {
                    Content = scriptProvider.Scripts.Select(s => s.Name),
                    Success = true,
                    Message = "Ok"
                };
            }
            catch (Exception e)
            {
                Result = new ApiResult<IEnumerable<string>>
                {
                    Content = new string[0],
                    Success = false,
                    Message = "Error getting scripts",
                    ErrorMessage = e.Message
                };
            }
        }

    }
}
