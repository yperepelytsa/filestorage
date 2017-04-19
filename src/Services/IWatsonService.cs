using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace src.Services
{
    public interface IWatsonService
    {
        Task<string> CreateCollectionAsync(string name);
        Task<string> DeleteCollectionAsync(string name);
        Task<List<string>> SearchFiles(string name,string query);
        Task AddFileAsync(string name, string filename);
        Task DeleteFileAsync(string name, string file);
    }
}