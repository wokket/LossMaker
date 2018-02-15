using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LossMaker
{
    public static class FileDownloader
    {
        public static async Task DownloadFileAsync(string url, string localFileName, IProgress<double> progress, CancellationToken token)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(string.Format("The request returned with HTTP status code {0}", response.StatusCode));
                }

                var total = response.Content.Headers.ContentLength ?? -1L;
                var canReportProgress = total != -1 && progress != null;

                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    var totalRead = 0L;
                    var buffer = new byte[4096];
                    var isMoreToRead = true;

                    using (var localFile = File.OpenWrite(localFileName))
                    {
                        do
                        {
                            token.ThrowIfCancellationRequested();

                            var read = await stream.ReadAsync(buffer, 0, buffer.Length, token);

                            if (read == 0)
                            {
                                isMoreToRead = false;
                            }
                            else
                            {

                                await localFile.WriteAsync(buffer, 0, read);

                                totalRead += read;

                                if (canReportProgress)
                                {
                                    progress.Report((totalRead * 1d) / (total * 1d)); //report progress in [0..1] range
                                }
                            }
                        } while (isMoreToRead);
                    }
                }
            }
        }
    }
}
