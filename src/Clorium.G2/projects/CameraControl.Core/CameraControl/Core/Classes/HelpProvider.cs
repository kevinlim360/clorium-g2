using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using CameraControl.Devices;
using Ionic.Zip;

namespace CameraControl.Core.Classes;

public class HelpProvider
{
	private static Dictionary<HelpSections, string> _helpData;

	private static void Init()
	{
		_helpData = new Dictionary<HelpSections, string>
		{
			{
				HelpSections.MainMenu,
				"http://digicamcontrol.com/doc/userguide"
			},
			{
				HelpSections.Bracketig,
				"http://digicamcontrol.com/doc/userguide/interface/bracketing"
			},
			{
				HelpSections.FocusStacking,
				"http://digicamcontrol.com/doc/userguide/interface/liveview#focus-stacking"
			},
			{
				HelpSections.Settings,
				"http://digicamcontrol.com/doc/userguide/settings"
			},
			{
				HelpSections.TimeLapse,
				"http://digicamcontrol.com/doc/userguide/interface/timelapse"
			},
			{
				HelpSections.LiveView,
				"http://digicamcontrol.com/doc/userguide/interface/liveview"
			},
			{
				HelpSections.Session,
				"http://digicamcontrol.com/doc/userguide/session"
			},
			{
				HelpSections.Bulb,
				"http://digicamcontrol.com/doc/userguide/interface/astromod"
			},
			{
				HelpSections.MultipleCamera,
				"http://digicamcontrol.com/doc/userguide/interface/multicam"
			},
			{
				HelpSections.DownloadPhotos,
				"http://digicamcontrol.com/doc/userguide/interface/downph"
			}
		};
	}

	public static void Run(HelpSections sections)
	{
		if (_helpData == null)
		{
			Init();
		}
		PhotoUtils.Run(_helpData[sections], "");
	}

	public static void SendEmail(string body, string subject, string from, string to, string file = null)
	{
		using SmtpClient mailClient = new SmtpClient("smtp.sendgrid.net", 587);
		// Resolve SMTP credentials when email is sent so capture and other startup flows remain available without them.
		mailClient.Credentials = new NetworkCredential(RequiredCredential("CLORIUM_SENDGRID_USER"), RequiredCredential("CLORIUM_SENDGRID_PASSWORD"));
		MailMessage message = new MailMessage("admin@digicamcontrol.com", to)
		{
			Subject = subject,
			Body = (body ?? ""),
			IsBodyHtml = false
		};
		message.ReplyToList.Add(from);
		if (File.Exists(file))
		{
			message.Attachments.Add(new Attachment(file));
		}
		mailClient.Send(message);
		message.Dispose();
	}

	public static void SendCrashReport(string body, string type, string email = null)
	{
		try
		{
			string destfile = Path.Combine(Path.GetTempPath(), "error_report.zip");
			if (File.Exists(destfile))
			{
				File.Delete(destfile);
			}
			using (ZipFile zip = new ZipFile(destfile))
			{
				zip.AddFile(ServiceProvider.LogFile, "");
				zip.Save(destfile);
			}
			body = "Version :" + Assembly.GetExecutingAssembly().GetName().Version?.ToString() + "\nClient Id" + ServiceProvider.Settings.ClientId + "\n" + body;
			SendEmail(body, type ?? "Log file", string.IsNullOrWhiteSpace(email) ? "admin@digicamcontrol.com" : email, "error_report@digicamcontrol.com", destfile);
		}
		catch (Exception exception)
		{
			Log.Error("Error send email", exception);
		}
	}
	private static string RequiredCredential(string variableName)
	{
		string value = Environment.GetEnvironmentVariable(variableName);
		if (string.IsNullOrWhiteSpace(value))
		{
			throw new InvalidOperationException("Required mail credential is missing: " + variableName);
		}
		return value;
	}

}
