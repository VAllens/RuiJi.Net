﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuiJi.Net.Storage
{
    public class MySqlStorage : StorageBase
    {
        public MySqlStorage(string connectString, string databaseName = "", string collectionName = "") : base(connectString, databaseName, collectionName)
        {
        }

        public override int Insert(IContentModel content)
        {
            throw new NotImplementedException();
        }

        public override int Insert(IContentModel[] contents)
        {
            throw new NotImplementedException();
        }

        public override bool Remove(int id)
        {
            throw new NotImplementedException();
        }

        public override bool Remove(string url)
        {
            throw new NotImplementedException();
        }

        public override bool Update(IContentModel content)
        {
            throw new NotImplementedException();
        }
    }
}