using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace LivingDevAgent.Editor.Scribe
    {
    /// <summary>
    /// Image management and caching system
    /// KeeperNote: This is the "art vault" - manages all visual assets with intelligent caching
    /// </summary>
    public class ScribeImageManager
        {
        private readonly Dictionary<string, Texture2D> _imageCache = new();
        private readonly List<string> _cacheOrder = new();
        private const int MaxCacheSize = 128;

        // Supported image extensions
        private readonly HashSet<string> _supportedExtensions = new()
        {
            ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tga"
        };

        public List<string> ImagePaths { get; } = new();

        public Texture2D GetTexture(string path)
            {
            if (string.IsNullOrEmpty(path))
                return null;

            // Check cache first
            if (_imageCache.TryGetValue(path, out Texture2D cached))
                {
                // Move to end of LRU list
                _cacheOrder.Remove(path);
                _cacheOrder.Add(path);
                return cached;
                }

            // Try to load texture
            Texture2D texture = LoadTexture(path);
            if (texture != null)
                {
                AddToCache(path, texture);
                }

            return texture;
            }

        Texture2D LoadTexture(string path)
            {
            string fullPath = ResolveImagePath(path);

            if (!File.Exists(fullPath))
                return null;

            try
                {
                byte[] bytes = File.ReadAllBytes(fullPath);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

                if (texture.LoadImage(bytes))
                    {
                    return texture;
                    }
                else
                    {
                    Object.DestroyImmediate(texture);
                    return null;
                    }
                }
            catch (System.Exception e)
                {
                Debug.LogWarning($"[ScribeImageManager] Failed to load image: {path}\n{e.Message}");
                return null;
                }
            }

        void AddToCache(string path, Texture2D texture)
            {
            // KeeperNote: LRU cache eviction prevents unbounded memory growth
            if (_cacheOrder.Count >= MaxCacheSize)
                {
                // Remove oldest
                string oldest = _cacheOrder[0];
                _cacheOrder.RemoveAt(0);

                if (_imageCache.TryGetValue(oldest, out Texture2D oldTexture))
                    {
                    Object.DestroyImmediate(oldTexture);
                    _imageCache.Remove(oldest);
                    }
                }

            _imageCache[path] = texture;
            _cacheOrder.Add(path);
            }

        public void ClearCache()
            {
            foreach (Texture2D texture in _imageCache.Values)
                {
                if (texture != null)
                    {
                    Object.DestroyImmediate(texture);
                    }
                }

            _imageCache.Clear();
            _cacheOrder.Clear();
            }

        public string AddImage(string sourcePath)
            {
            if (!File.Exists(sourcePath))
                return null;

            string extension = Path.GetExtension(sourcePath).ToLower();
            if (!_supportedExtensions.Contains(extension))
                {
                Debug.LogWarning($"[ScribeImageManager] Unsupported image format: {extension}");
                return null;
                }

            // Copy to images directory
            string imagesDir = GetImagesDirectory();
            string fileName = Path.GetFileName(sourcePath);
            string destPath = Path.Combine(imagesDir, fileName);

            // Handle duplicates
            destPath = GetUniqueFilePath(destPath);

            try
                {
                File.Copy(sourcePath, destPath, false);

                // Import if in Assets folder
                ImportAssetIfNeeded(destPath);

                // Add to tracked paths
                string relativePath = GetRelativeImagePath(destPath);
                if (!ImagePaths.Contains(relativePath))
                    {
                    ImagePaths.Add(relativePath);
                    }

                return relativePath;
                }
            catch (System.Exception e)
                {
                Debug.LogError($"[ScribeImageManager] Failed to add image: {e.Message}");
                return null;
                }
            }

        public string GetImagesDirectory()
            {
            // KeeperNote: Centralized images folder ensures predictable asset organization
            string rootPath = EditorPrefs.GetString("LDA_SCRIBE_ROOT", "");
            if (string.IsNullOrEmpty(rootPath))
                {
                rootPath = Path.Combine(Application.dataPath, "Plugins/TLDA/docs");
                }

            string imagesDir = Path.Combine(rootPath, "images");
            if (!Directory.Exists(imagesDir))
                {
                Directory.CreateDirectory(imagesDir);
                }

            return imagesDir;
            }

        string GetUniqueFilePath(string path)
            {
            if (!File.Exists(path))
                return path;

            string dir = Path.GetDirectoryName(path);
            string name = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);

            int counter = 1;
            string newPath;
            do
                {
                newPath = Path.Combine(dir, $"{name}_{counter}{ext}");
                counter++;
                }
            while (File.Exists(newPath));

            return newPath;
            }

        string ResolveImagePath(string path)
            {
            if (Path.IsPathRooted(path) && File.Exists(path))
                return path;

            // Try relative to images directory
            string imagesDir = GetImagesDirectory();
            string fullPath = Path.Combine(imagesDir, path);
            if (File.Exists(fullPath))
                return fullPath;

            // Try relative to project root
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            fullPath = Path.Combine(projectRoot, path);
            if (File.Exists(fullPath))
                return fullPath;

            return path;
            }

        string GetRelativeImagePath(string absolutePath)
            {
            string imagesDir = GetImagesDirectory();
            if (absolutePath.StartsWith(imagesDir))
                {
                string relative = absolutePath[imagesDir.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return "images/" + relative.Replace('\\', '/');
                }

            return Path.GetFileName(absolutePath);
            }

        void ImportAssetIfNeeded(string path)
            {
            string dataPath = Application.dataPath.Replace('\\', '/');
            string normalizedPath = path.Replace('\\', '/');

            if (normalizedPath.StartsWith(dataPath))
                {
                string assetPath = "Assets" + normalizedPath[dataPath.Length..];
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
                }
            }
        }
    }
