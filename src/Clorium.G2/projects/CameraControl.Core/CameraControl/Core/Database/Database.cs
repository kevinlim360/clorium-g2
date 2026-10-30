using System;
using System.Collections.Generic;
using System.Linq;
using CameraControl.Core.Classes;
using CameraControl.Devices;
using SQLite;

namespace CameraControl.Core.Database;

public class Database : SQLiteConnection
{
	public Database(string path)
		: base(path)
	{
		CreateTable<DbEvents>();
		CreateTable<DbFile>();
	}

	public void Add(DbEvents events)
	{
		if (!ServiceProvider.Settings.UseDatabase)
		{
			return;
		}
		try
		{
			Insert(events);
		}
		catch (Exception exception)
		{
			Log.Error("", exception);
		}
	}

	public void Add(DbFile file)
	{
		if (!ServiceProvider.Settings.UseDatabase)
		{
			return;
		}
		try
		{
			Insert(file);
		}
		catch (Exception exception)
		{
			Log.Error("", exception);
		}
	}

	public void StartEvent(EventType eventType)
	{
		if (!ServiceProvider.Settings.UseDatabase)
		{
			return;
		}
		try
		{
			Insert(new DbEvents(eventType));
		}
		catch (Exception exception)
		{
			Log.Error("Insert error", exception);
		}
	}

	public void EndEvent(EventType _eventType)
	{
		if (!ServiceProvider.Settings.UseDatabase)
		{
			return;
		}
		try
		{
			DbEvents e = Table<DbEvents>().Last((DbEvents x) => x.EventType == EventType.App);
			if (!e.EndDate.HasValue)
			{
				e.EndDate = DateTime.Now;
				Update(e);
			}
		}
		catch (Exception exception)
		{
			Log.Error("Insert error", exception);
		}
	}

	public IEnumerable<DbEvents> GetApp(DateTime from, DateTime to)
	{
		if (!ServiceProvider.Settings.UseDatabase)
		{
			return null;
		}
		List<DbEvents> res = (from x in Table<DbEvents>()
			where (int)x.EventType == 0
			select x).ToList();
		return res.Where((DbEvents x) => x.EndDate.HasValue && x.StartDate.HasValue && x.StartDate.Value >= from && x.EndDate.Value.Date <= to.Date);
	}

	public IEnumerable<DbFile> GetFiles(DateTime from, DateTime to)
	{
		if (!ServiceProvider.Settings.UseDatabase)
		{
			return null;
		}
		List<DbFile> res = Table<DbFile>().ToList();
		return res.Where((DbFile x) => x.Date.HasValue && x.Date.Value.Date >= from.Date && x.Date.Value.Date <= to.Date);
	}

	public void SaveFile(FileItem fileItem)
	{
		if (ServiceProvider.Settings.UseDatabase)
		{
			DbFile e = Table<DbFile>().LastOrDefault((DbFile x) => x.FileId == fileItem.Id);
			if (e == null)
			{
				DbFile dbfile = new DbFile(fileItem, "", "", ServiceProvider.Settings.DefaultSession.Name);
				Insert(dbfile);
			}
			else
			{
				e.Copy(fileItem);
				Update(e);
			}
		}
	}
}
