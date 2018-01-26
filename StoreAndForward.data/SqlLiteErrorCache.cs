using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace StoreAndForward.Cache
{
    /// <summary>
    /// Error Cache leveraging SQLite
    /// </summary>
    public class SqlLiteErrorCache
    {
        //Sets the name of the folder where the Sqlite db will reside inside of the local data folder
        private string _cacheName;

        //Connection to the SQLlite DB
        private SQLiteAsyncConnection _connection;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cacheName">name of the folder where the Sqlite Db will reside inside of the local Application Data location. Default is "virtus.errors" </param>
        public SqlLiteErrorCache(string cacheName = null)
        {
            if (cacheName != null)
            {
                _cacheName = cacheName;
            }
            else
            {
                _cacheName = "storeandforward";
            }
            SetUpDB();
        }

        /// <summary>
        /// Sets up the DB connection to the SQLite DB
        /// </summary>
        private void SetUpDB()
        {
            //Get reference to the App Data Folder
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            //Add the folder for the cache name
            var folderPath = Path.Combine(appData, _cacheName);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            //Combine cache folder with the errors.db SQLite file
            var dbPath = Path.Combine(folderPath, "errors.db");

            //Connect to the DB
            _connection = new SQLiteAsyncConnection(dbPath);

            //Create the table in the DB if it doesn't already exist
            _connection.CreateTableAsync<MessageCacheDto>().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Sets the name of the cache. Can be used instead of the parameter in the constructor
        /// </summary>
        /// <param name="cacheName">name of the folder in Local Storage where the DB will be created</param>
        public void SetCacheName(string cacheName)
        {
            _cacheName = cacheName;
            SetUpDB();
        }

        /// <summary>
        /// Add an item to the SQLite DB
        /// </summary>
        /// <param name="cacheObject">item to add</param>
        /// <returns></returns>
        public async Task Add(MessageCacheDto cacheObject)
        { 
            await _connection.InsertAsync(cacheObject);
        }

        /// <summary>
        /// Removes all items from the SQLite error cache
        /// </summary>
        /// <returns></returns>
        public async Task Clear()
        {
            var allItems = await GetAll();
            foreach (var itm in allItems)
            {
                await _connection.DeleteAsync(itm);
            }
        }

        /// <summary>
        /// Returns all items in the cache
        /// </summary>
        /// <returns>List of items in the cache</returns>
        public async Task<List<MessageCacheDto>> GetAll()
        {
            var query = _connection.Table<MessageCacheDto>();
            var result = await query.ToListAsync();
            return result;
        }

        /// <summary>
        /// Gets the number of cached items
        /// </summary>
        /// <returns>number of cached items</returns>
        public int Count()
        {
            return _connection.Table<MessageCacheDto>().CountAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Remove an item from the cache
        /// </summary>
        /// <param name="itemToSend">Item to remove</param>
        /// <returns></returns>
        public async Task<int> Remove(MessageCacheDto itemToRemove)
        {
            return await _connection.DeleteAsync(itemToRemove);
        }
    }
}
