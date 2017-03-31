using LibraryInstaller.Contracts;
using LibraryInstaller.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using LibraryInstaller.Providers.Cdnjs;

namespace LibraryInstaller.Test.Providers.Cdnjs
{
    [TestClass]
    public class CdnjsProviderTest
    {
        private string _cacheFolder;
        private string _projectFolder;
        private IDependencies _dependencies;

        [TestInitialize]
        public void Setup()
        {
            _cacheFolder = Environment.ExpandEnvironmentVariables(@"%localappdata%\Microsoft\Library\");
            _projectFolder = Path.Combine(Path.GetTempPath(), "LibraryInstaller");
            var hostInteraction = new HostInteraction(_projectFolder, _cacheFolder);
            _dependencies = new Dependencies(hostInteraction, new CdnjsProvider());

            Directory.CreateDirectory(_projectFolder);
        }

        [TestCleanup]
        public void Cleanup()
        {
            File.Delete(Path.Combine(_dependencies.GetHostInteractions().CacheDirectory, "cdnjs", "cache.json"));
            Directory.Delete(_projectFolder, true);
        }

        [TestMethod]
        public async Task EndToEndTestAsync()
        {
            IProvider provider = _dependencies.GetProvider("cdnjs");
            ILibraryCatalog catalog = provider.GetCatalog();

            // Search for libraries to display in search result
            IReadOnlyList<ILibraryGroup> groups = await catalog.SearchAsync("jquery", 4, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(4, groups.Count);

            // Show details for selected library
            ILibraryGroup group = groups.FirstOrDefault();
            Assert.AreEqual("jquery", group.Name);

            // Get all libraries in group to display version list
            IReadOnlyList<ILibraryDisplayInfo> displayInfos = await group.GetDisplayInfosAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.IsTrue(displayInfos.Count >= 67);
            Assert.AreEqual("1.2.3", displayInfos.ElementAt(displayInfos.Count - 1).Version, "Library version mismatch");

            // Get the library to install
            ILibraryDisplayInfo displayInfo = displayInfos.FirstOrDefault();
            ILibrary library = await displayInfo.GetLibraryAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(group.Name, library.Id);
            Assert.AreEqual(displayInfo.Version, library.Version);

            var desiredState = new LibraryInstallationState
            {
                LibraryId = "jquery@3.1.1",
                ProviderId = "cdnjs",
                Path = "lib",
                Files = new[] { "jquery.js", "jquery.min.js" }
            };

            // Install library
            ILibraryInstallationResult result = await provider.InstallAsync(desiredState, CancellationToken.None).ConfigureAwait(false);

            foreach (string file in desiredState.Files)
            {
                string absolute = Path.Combine(_projectFolder, desiredState.Path, file);
                Assert.IsTrue(File.Exists(absolute));
            }

            Assert.IsTrue(result.Success);
            Assert.IsFalse(result.Cancelled);
            Assert.AreSame(desiredState, result.InstallationState);
            Assert.AreEqual(0, result.Errors.Count);
        }

        [TestMethod]
        public async Task InvalidLibraryAsync()
        {
            IProvider provider = _dependencies.GetProvider("cdnjs");

            var desiredState = new LibraryInstallationState
            {
                LibraryId = "*&(}:@3.1.1",
                ProviderId = "cdnjs",
                Path = "lib",
                Files = new[] { "jquery.min.js" }
            };

            // Install library
            ILibraryInstallationResult result = await provider.InstallAsync(desiredState, CancellationToken.None).ConfigureAwait(false);
            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        public async Task InvalidLibraryFilesAsync()
        {
            IProvider provider = _dependencies.GetProvider("cdnjs");

            var desiredState = new LibraryInstallationState
            {
                LibraryId = "jquery@3.1.1",
                ProviderId = "cdnjs",
                Path = "lib",
                Files = new[] { "file1.txt", "file2.txt" }
            };

            // Install library
            ILibraryInstallationResult result = await provider.InstallAsync(desiredState, CancellationToken.None).ConfigureAwait(false);
            Assert.IsFalse(result.Success);
            Assert.AreEqual("LIB003", result.Errors[0].Code);
        }

        private const string _doc = @"{
  ""version"": ""1.0"",
  ""packages"": [
    {
      ""provider"": ""cdnjs"",
      ""id"": ""jquery@3.1.1"",
      ""path"": ""lib"",
      ""files"": [ ""jquery.js"", ""jquery.min.js"" ]
    },
    {
      ""provider"": ""cdnjs"",
      ""id"": ""knockout@3.4.1"",
      ""path"": ""lib"",
      ""files"": [ ""knockout-min.js"" ]
    }
  ]
}
";
    }
}
