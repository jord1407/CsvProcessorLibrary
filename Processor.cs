﻿using CsvProcessor.Processors.Attributes;
using CsvProcessor.Processors.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace CsvProcessor.Processors
{
    public class Processor<T> where T : Enum
    {
        #region Private Properties

        private readonly IProcessor _processor;
        private readonly string[] _contents;
        private readonly char _seperator;

        #endregion

        #region Constructor

        public Processor(string file, T fileType, char separator = ',')
        {
            _processor = new ProcessorFactory<T>(fileType).Processor;
            _contents = File.ReadAllLines(file, Encoding.GetEncoding("iso-8859-1"));
            _seperator = separator;
        }

        #endregion

        #region Public Methods

        public void Execute(bool header = false)
        {
            DataTable dt = ToDataTable(header);
            _processor?.Process(dt);
        }

        public IEnumerable<S> Execute<S>(bool header = false) where S : class
        {
            DataTable dt = ToDataTable(header);
            List<S> list = new List<S>();

            foreach (DataRow row in dt.Rows)
            {
                S item = (S)Activator.CreateInstance(typeof(S));

                if (!header)
                {

                    foreach (var property in item.GetType().GetProperties())
                    {
                        var index = ((CsvAttribute)property.GetCustomAttributes(typeof(CsvAttribute), false)[0]).Position;
                        property.SetValue(item, Convert.ChangeType(row[$"Column{index}"], property.PropertyType));
                    }
                }
                else
                {
                    string[] columns = _contents?[0].Replace("\"", string.Empty).Trim().Split(',');

                    foreach (var property in item.GetType().GetProperties())
                    {
                        var index = ((CsvAttribute)property.GetCustomAttributes(typeof(CsvAttribute), false)[0]).Position;
                        property.SetValue(item, Convert.ChangeType(row[columns[Convert.ToInt32(index)]], property.PropertyType));
                    }
                }

                list.Add(item);
            }

            return list;
        }

        public List<string> ConvertContentToCSV<S>(List<S> list) where S: class
        {
            List<string> lines = new List<string>();
            foreach(S item in list)
            {
                string line = string.Empty;

                var properties = item.GetType().GetProperties();
                for (int i = 0; i < properties.Length; i++ )
                {
                    line += properties[i].GetValue(item).ToString();
                    if (i < properties.Length - 1)
                        line += _seperator;
                }

                lines.Add(line);
            }

            return lines;
        }

        public DataTable GetContentAsDataTable()
        {
            DataTable dt = ToDataTable();
            return dt;
        }

        #endregion

        #region Private Methods

        private DataTable ToDataTable(bool header = false)
        {
            if (!header)
                return ToDataTableWithoutHeader();
            else
                return ToDataTableWithHeader();
        }

        private DataTable ToDataTableWithoutHeader()
        {
            DataTable dt = new DataTable();
            
            int index = 0;
            string[] columns = _contents?[0].Replace("\"", string.Empty).Trim().Split(',');

            foreach (string column in columns)
            {
                dt.Columns.Add($"Column{index++}");
            }

            foreach (string line in _contents)
            {
                DataRow dr = dt.NewRow();
                index = 0;

                foreach (string column in line.Split(_seperator))
                {
                    dr[$"Column{index++}"] = column.Trim();
                }

                dt.Rows.Add(dr);
            }

            return dt;
        }

        private DataTable ToDataTableWithHeader()
        {
            DataTable dt = new DataTable();

            int index = 0;
            string[] columns = _contents?[0].Replace("\"", string.Empty).Trim().Split(',');

            foreach (string column in columns)
            {
                dt.Columns.Add(column);
            }

            string[] contents = _contents?.Skip(1).ToArray();

            foreach (string line in contents)
            {
                DataRow dr = dt.NewRow();
                index = 0;

                foreach (string column in line.Split(_seperator))
                {
                    dr[columns[index++]] = column.Trim();
                }

                dt.Rows.Add(dr);
            }

            return dt;
        }

        #endregion
    }
}