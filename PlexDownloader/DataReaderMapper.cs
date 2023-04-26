using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Reflection;


namespace PlexDownloader
{
    public class DataReaderMapper<T>
    {
        public T MapObjectFromReader(SQLiteDataReader reader)
        {
            T obj = Activator.CreateInstance<T>();
            if (reader != null && reader.HasRows)
            {
                if (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string fieldName = reader.GetName(i);
                        object fieldValue = reader.GetValue(i);
                        PropertyInfo property = typeof(T).GetProperty(fieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                        if (property != null && fieldValue != DBNull.Value)
                        {
                            property.SetValue(obj, fieldValue);
                        }
                    }
                }
            }
            return obj;
        }
        public T MapObjectFromReaderNoRead(SQLiteDataReader reader)
        {
            T obj = Activator.CreateInstance<T>();
            if (reader != null && reader.HasRows)
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string fieldName = reader.GetName(i);
                    object fieldValue = reader.GetValue(i);
                    PropertyInfo property = typeof(T).GetProperty(fieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    if (property != null && fieldValue != DBNull.Value)
                    {
                        property.SetValue(obj, fieldValue);
                    }
                }
            }
            return obj;
        }
        public List<T> MapObjectsFromReader(SQLiteDataReader reader)
        {
            List<T> objects = new List<T>();
            if (reader != null && reader.HasRows)
            {
                while (reader.Read())
                {
                    T obj = MapObjectFromReaderNoRead(reader);
                    objects.Add(obj);
                }
            }
            return objects;
        }
    }
}
