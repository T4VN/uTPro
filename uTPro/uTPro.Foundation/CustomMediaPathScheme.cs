using Microsoft.Extensions.Logging;
using System.Globalization;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.IO.MediaPathSchemes;
using uTPro.Extension.FileHelper;

namespace uTPro.Foundation
{
    class CustomMediaPathScheme : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            //Default by umbraco
            builder.Services.AddUnique<IMediaPathScheme, OriginalMediaPathScheme>();
            //builder.Services.AddUnique<IMediaPathScheme, Umbraco.Cms.Core.IO.MediaPathSchemes.TwoGuidsMediaPathScheme>();
            //builder.Services.AddUnique<IMediaPathScheme, Umbraco.Cms.Core.IO.MediaPathSchemes.CombinedGuidsMediaPathScheme>();
            //builder.Services.AddUnique<IMediaPathScheme, Umbraco.Cms.Core.IO.MediaPathSchemes.UniqueMediaPathScheme>();
        }
    }

    /// <summary>
    /// Implements the original media path scheme.
    /// </summary>
    /// <remarks>
    /// <para>Path is "{number}/{filename}" or "{number}-{filename}" where number is an incremented counter.</para>
    /// <para>Use '/' or '-' depending on UploadAllowDirectories setting.</para>
    /// </remarks>
    // scheme: path is "<number>/<filename>" where number is an incremented counter
    public class OriginalMediaPathScheme : UniqueMediaPathScheme, IMediaPathScheme
    {
        private readonly object _folderCounterLock = new object();
        private long _folderCounter;
        private bool _folderCounterInitialized;
        private readonly ILogger<OriginalMediaPathScheme> _logger;

        public OriginalMediaPathScheme(ILogger<OriginalMediaPathScheme> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public new string GetFilePath(MediaFileManager fileSystem, Guid itemGuid, Guid propertyGuid, string filename)
        {
            try
            {
                string type = GetNameFolderType(filename);
                //string previous = null;
                //string directory;
                //if (previous != null)
                //{
                //    // old scheme, with a previous path
                //    // prevpath should be "<int>/<filename>" OR "<int>-<filename>"
                //    // and we want to reuse the "<int>" part, so try to find it

                //    const string sep = "/";
                //    var pos = previous.IndexOf(sep, StringComparison.Ordinal);
                //    var s = pos > 0 ? previous.Substring(0, pos) : null;

                //    directory = pos > 0 && int.TryParse(s, out _) ? s : GetNextDirectory(fileSystem.FileSystem, type);
                //}
                //else
                //{
                //    directory = GetNextDirectory(fileSystem.FileSystem, type);
                //}

                //if (directory == null)
                //    throw new InvalidOperationException("Cannot use a null directory.");

                Guid combinedGuid = GuidUtils.Combine(itemGuid, propertyGuid);
                var guidFile = GuidUtils.ToBase32String(combinedGuid, 4);

                return Path.Combine(type, guidFile + "-" + FileNameHelper.GetValidFileName(filename)).Replace('\\', '/');
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return base.GetFilePath(fileSystem, itemGuid, propertyGuid, filename);
            }
        }

        /// <inheritdoc />
        //public string? GetDeleteDirectory(MediaFileManager fileSystem, string filepath)
        //{
        //    return null;
        //}

        private string GetNextDirectory(IFileSystem fileSystem, string type)
        {
            EnsureFolderCounterIsInitialized(fileSystem, type);
            return Interlocked.Increment(ref _folderCounter).ToString(CultureInfo.InvariantCulture);
        }

        private void EnsureFolderCounterIsInitialized(IFileSystem fileSystem, string type)
        {
            lock (_folderCounterLock)
            {
                if (_folderCounterInitialized) return;

                _folderCounter = 1000; // seed
                var directories = fileSystem.GetDirectories(type);
                foreach (var directory in directories)
                {
                    if (long.TryParse(directory, out var folderNumber) && folderNumber > _folderCounter)
                        _folderCounter = folderNumber;
                }

                // note: not multi-domains ie LB safe as another domain could create directories
                // while we read and parse them - don't fix, move to new scheme eventually

                _folderCounterInitialized = true;
            }
        }

        private string GetMimeMapping(string fileName)
        {
            return MimeTypeMap.TryGetMimeType(fileName, out var result) ? result : "unknown";
        }

        private string GetNameFolderType(string type)
        {
            return GetMimeMapping(type).Split('/')?.FirstOrDefault() ?? "unknown";
        }
    }
}
