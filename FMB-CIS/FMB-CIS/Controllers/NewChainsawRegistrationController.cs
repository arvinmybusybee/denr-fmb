﻿
using System;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using FMB_CIS.Data;
using FMB_CIS.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Web.Helpers;
using Microsoft.Extensions.Hosting;

namespace FMB_CIS.Controllers
{
    public class NewChainsawRegistrationController : Controller
    {
        private readonly LocalContext _context;
        private readonly IConfiguration _configuration;
        private IEmailSender EmailSender { get; set; }


        public NewChainsawRegistrationController(IConfiguration configuration, LocalContext context, IEmailSender emailSender)
        {
            this._configuration = configuration;
            _context = context;
            EmailSender = emailSender;
        }
        public IActionResult Index()
        {
            string host = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/";
            ViewData["BaseUrl"] = host;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(ViewModel model)
        {
            //try
            //{
            if (ModelState.IsValid)
            {

                int userID = Convert.ToInt32(((ClaimsIdentity)User.Identity).FindFirst("userID").Value);
                //DAL dal = new DAL();

                //SAVE permit application
                model.tbl_Application.tbl_application_type_id = 3;
                model.tbl_Application.status = 1;
                model.tbl_Application.tbl_user_id = userID;
                model.tbl_Application.tbl_permit_type_id = 13;
                model.tbl_Application.is_active = true;
                model.tbl_Application.created_by = userID;
                model.tbl_Application.modified_by = userID;
                model.tbl_Application.date_created = DateTime.Now;
                model.tbl_Application.date_modified = DateTime.Now;

                //SAVE to tbl_chainsaw
                model.TBL_Chainsaw.user_id = userID;
                model.TBL_Chainsaw.status = "Owner";
                model.TBL_Chainsaw.is_active = true;
                model.TBL_Chainsaw.created_by = userID;
                model.TBL_Chainsaw.modified_by = userID;
                model.TBL_Chainsaw.date_created = DateTime.Now;
                model.TBL_Chainsaw.date_modified = DateTime.Now;

                model.TBL_Chainsaw.chainsaw_date_of_registration = DateTime.Now;
                model.TBL_Chainsaw.chainsaw_date_of_expiration = DateTime.Now.AddYears(3);

                if (model.TBL_Chainsaw.Power == "Gas")
                {
                    model.TBL_Chainsaw.watt = null;
                }
                else if (model.TBL_Chainsaw.Power == "Electric" || model.TBL_Chainsaw.Power == "Battery")
                {
                    model.TBL_Chainsaw.hp = null;
                }
                _context.tbl_application.Add(model.tbl_Application);
                _context.tbl_chainsaw.Add(model.TBL_Chainsaw);
                _context.SaveChanges();
                int? appID = model.tbl_Application.id;

                //File Upload
                if (model.filesUpload != null)
                {
                    foreach (var file in model.filesUpload.Files)
                    {
                        var filesDB = new tbl_files();
                        FileInfo fileInfo = new FileInfo(file.FileName);
                        string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Files/UserDocs");

                        //create folder if not exist
                        if (!Directory.Exists(path))
                            Directory.CreateDirectory(path);


                        string fileNameWithPath = Path.Combine(path, file.FileName);

                        using (var stream = new FileStream(fileNameWithPath, FileMode.Create))
                        {
                            file.CopyTo(stream);
                        }
                        filesDB.tbl_application_id = appID;
                        filesDB.created_by = userID;
                        filesDB.modified_by = userID;
                        filesDB.date_created = DateTime.Now;
                        filesDB.date_modified = DateTime.Now;
                        filesDB.filename = file.FileName;
                        filesDB.path = path;
                        filesDB.tbl_file_type_id = fileInfo.Extension;
                        filesDB.tbl_file_sources_id = fileInfo.Extension;
                        filesDB.file_size = Convert.ToInt32(file.Length);
                        _context.tbl_files.Add(filesDB);
                        _context.SaveChanges();
                    }
                }
                //Email
                var subject = "Chainsaw Registration Application Status";
                var body = "Greetings! \n We would like to inform you that your Chainsaw Registration Application has been received.";
                EmailSender.SendEmailAsync(((ClaimsIdentity)User.Identity).FindFirst("EmailAdd").Value, subject, body);

                ModelState.Clear();
                ViewBag.Message = "Save Success";
                return View();
            }
            return View(model);
            //}
            //catch
            //{
            //    return View(model);
            //    //return RedirectToAction("Index", "Dashboard");
            //}
        }

        [HttpPost, ActionName("CheckExistingSerialNumOnField")]
        public JsonResult CheckExistingSerialNumOnField(string serialNum)
        {
            var chainsawSerialNumExist = _context.tbl_chainsaw.Where(m => m.chainsaw_serial_number == serialNum).Any();
            return Json(chainsawSerialNumExist);
            
        }
    }
}
