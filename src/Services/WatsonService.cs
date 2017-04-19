using Newtonsoft.Json;
using src.Data;
using src.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace src.Services
{
    public class WatsonService : IWatsonService
    {
        private readonly Creds _creds;
        private readonly ApplicationDbContext _context;
        public WatsonService(Creds creds, ApplicationDbContext context)
        {
            _creds = creds;
            _context = context;
        }

        public async Task<string> CreateCollectionAsync(string name)
        {
            using (var client = WatsonClient())
            {
                var uploads = Path.Combine("../uploads", name);
                if (!Directory.Exists(uploads))
                    Directory.CreateDirectory(uploads);
                var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(_creds.username + ":" + _creds.password));
                var req = new HttpRequestMessage(HttpMethod.Post, "https://gateway.watsonplatform.net/discovery/api/v1/environments/" + _creds.defenvironmentid + "/collections?version=2016-12-01");
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                req.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
                var postData = new List<KeyValuePair<string, string>>();
                postData.Add(new KeyValuePair<string, string>("name", name));
                req.Content = new FormUrlEncodedContent(postData);
                var response = await client.SendAsync(req);
                var resstr = await response.Content.ReadAsStringAsync();
                Console.WriteLine(resstr);
                var collection = JsonConvert.DeserializeObject<Collection>(resstr);
                collection.userid = name;
                _context.Collections.Add(collection);
                _context.SaveChanges();
                return resstr;
            }
        }
       
        public async Task<string> DeleteCollectionAsync(string name)
        {
            using (var client = WatsonClient())
            {
                var colid = _context.Collections.FirstOrDefault(c => c.userid == name).collection_id;
                var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(_creds.username + ":" + _creds.password));
                var req = new HttpRequestMessage(HttpMethod.Delete, "https://gateway.watsonplatform.net/discovery/api/v1/environments/" + _creds.defenvironmentid + "/collections/"+colid+"?version=2016-12-01");
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                req.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
                var response = await client.SendAsync(req);
                return await response.Content.ReadAsStringAsync();
            }
        }


        public async Task AddFileAsync(string name, string filename)
        {
            using (var client = WatsonClient())
            {
                var colid = _context.Collections.FirstOrDefault(c => c.userid == name).collection_id;
                var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(_creds.username + ":" + _creds.password));
                var req = new HttpRequestMessage(HttpMethod.Post, "https://gateway.watsonplatform.net/discovery/api/v1/environments/" + _creds.defenvironmentid + "/collections/" + colid + "/documents?version=2016-12-01");
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("multipart/form-data"));
                req.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
                HttpContent bytesContent = new ByteArrayContent(File.ReadAllBytes(Path.Combine("../uploads", name, filename)));
                using (var formData = new MultipartFormDataContent())
                {
                    formData.Add(bytesContent, "file", filename);
                    req.Content = formData;
                    var response = await client.SendAsync(req);
                    if (!response.IsSuccessStatusCode)
                        throw new Exception();
                    var resstr = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(resstr);
                    var resobj = JsonConvert.DeserializeObject<DocResponse>(resstr);
                    if (resobj != null)
                    {
                        _context.Documents.Add(new Document() { document_id = resobj.document_id, filename = filename });
                        _context.SaveChanges();
                    }
                }
            }
        }

        public async Task DeleteFileAsync(string name, string file)
        {
            using (var client = WatsonClient())
            {
                var colid = _context.Collections.FirstOrDefault(c => c.userid == name).collection_id;
                var docid = _context.Documents.FirstOrDefault(d => d.filename == file).document_id;
                var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(_creds.username + ":" + _creds.password));
                var req = new HttpRequestMessage(HttpMethod.Delete, "https://gateway.watsonplatform.net/discovery/api/v1/environments/" + _creds.defenvironmentid + "/collections/" + colid + "/documents/"+ docid + "/?version=2016-12-01");
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                req.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
                var response = await client.SendAsync(req);
                if (response.IsSuccessStatusCode)
                {
                    var todelete = _context.Documents.FirstOrDefault(d => d.filename == file);
                    _context.Documents.Remove(todelete);
                    _context.SaveChanges();
                }
            }
        }

        public async Task<List<string>> SearchFiles(string name, string query)
        {
            if (query == null)
            {
                return _context.Documents.Select(d=>d.filename).ToList();
            }
            using (var client = WatsonClient())
            {
                var colid = _context.Collections.FirstOrDefault(c => c.userid == name).collection_id;
                var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(_creds.username + ":" + _creds.password));
                var req = new HttpRequestMessage(HttpMethod.Get, "https://gateway.watsonplatform.net/discovery/api/v1/environments/" + _creds.defenvironmentid + "/collections/" + colid + "/query?version=2016-12-01&query="+query+"&return=id");
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                req.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
                var dt = DateTime.Now;
                var response = await client.SendAsync(req);
                var resstr = await response.Content.ReadAsStringAsync();
                Console.WriteLine(resstr);
                var resobj = JsonConvert.DeserializeObject<QueryResult>(resstr);
                var res = resobj.results.Select(r => r.id).ToList();
                if (res != null) {
                    var reslist = _context.Documents.Where(d => res.Contains(d.document_id));
                    if (reslist.Count() == 0)
                        return new List<string>();
                    res = reslist.Select(d => d.filename).ToList();
                    return res;
                }
                return null;
            }
        }




        private HttpClient WatsonClient()
        {
            if (_creds.username == null || _creds.password == null || _creds.host == null)
            {
                throw new Exception("Missing Cloud service credentials");
            }

            var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(_creds.username + ":" + _creds.password));
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(_creds.host);
            return client;

        }

        
    }
}