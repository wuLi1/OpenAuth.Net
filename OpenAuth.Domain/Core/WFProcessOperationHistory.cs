﻿//------------------------------------------------------------------------------
// <autogenerated>
//     This code was generated by a CodeSmith Template.
//
//     DO NOT MODIFY contents of this file. Changes to this
//     file will be lost if the code is regenerated.
//     Author:Yubao Li
// </autogenerated>
//------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenAuth.Domain
{
    /// <summary>
	/// 
	/// </summary>
    public partial class WFProcessOperationHistory :Entity
    {
        public WFProcessOperationHistory()
        {
          this.ProcessId= string.Empty;
          this.Content= string.Empty;
          this.CreateDate= DateTime.Now;
          this.CreateUserId= string.Empty;
          this.CreateUserName= string.Empty;
        }

        /// <summary>
	    /// 
	    /// </summary>
        public string ProcessId { get; set; }
        /// <summary>
	    /// 
	    /// </summary>
        public string Content { get; set; }
        /// <summary>
	    /// 
	    /// </summary>
        public System.DateTime CreateDate { get; set; }
        /// <summary>
	    /// 
	    /// </summary>
        public string CreateUserId { get; set; }
        /// <summary>
	    /// 
	    /// </summary>
        public string CreateUserName { get; set; }

    }
}