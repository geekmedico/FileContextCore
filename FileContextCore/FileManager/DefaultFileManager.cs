using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.IO;
using FileContextCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace FileContextCore.FileManager
{
    public class DefaultFileManager : IFileManager
    {
        private readonly object _thisLock = new object();

        IEntityType _type;
        private string _filetype;
		private string _databasename;
        private string _location;
        private List<Guid> lista = new List<Guid>();
        public DefaultFileManager() { }
        
        public void Initialize(IFileContextScopedOptions options, IEntityType entityType, string fileType)
        {
            _type = entityType;
            _filetype = fileType;
            _databasename = options.DatabaseName ?? "";
            _location = options.Location;
        }

        public string GetFileName()
        {
            string name = _type.GetTableName().GetValidFileName();

            string path = string.IsNullOrEmpty(_location)
                ? Path.Combine(AppContext.BaseDirectory, "appdata", _databasename)
                : _location;

            Directory.CreateDirectory(path);

            return Path.Combine(path, name + "." + _filetype);
        }

        public string LoadContent()
        {
            lock (_thisLock)
            {
                string path = GetFileName();

                if (File.Exists(path))
                {
                    //Console.WriteLine("Lectura del archivo" + path);
                    return File.ReadAllText(path);
                }

                return "";
            }
        }
        //TODO: AQui necesitamos agregar un metodo para guardar los ID en la lista para saber que vamos a borrar
        //Pero debemos evitar guardar cosas innecesarias
        public void SaveContent(string content)
        {
            lock (_thisLock)
            {
                string path = GetFileName();
                //Console.WriteLine("Escritura del archivo" + path);
                File.WriteAllText(path, content);
            }
        }

        public bool Clear()
        {
            lock (_thisLock)
            {
                FileInfo fi = new FileInfo(GetFileName());

                if (fi.Exists)
                {
                    fi.Delete();
                    return true;
                }

                return false;
            }
        }

        public bool FileExists()
        {
            lock (_thisLock)
            {
                FileInfo fi = new FileInfo(GetFileName());

                return fi.Exists;
            }
        }
    }
}
