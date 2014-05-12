﻿// -----------------------------------------------------------------------------------------
// <copyright file="FileWriteStreamTest.cs" company="Microsoft">
//    Copyright 2013 Microsoft Corporation
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
// -----------------------------------------------------------------------------------------

using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace Microsoft.WindowsAzure.Storage.File
{
    [TestClass]
    public class FileWriteStreamTest : FileTestBase
    {
        [TestMethod]
        /// [Description("Create files using file stream")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileWriteStreamOpenAndCloseAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file");
                OperationContext opContext = new OperationContext();
                await TestHelper.ExpectedExceptionAsync(
                    async () => await file.OpenWriteAsync(null, null, null, opContext),
                    opContext,
                    "Opening a file stream with no size should fail on a file that does not exist",
                    HttpStatusCode.NotFound);
                using (IOutputStream writeStream = await file.OpenWriteAsync(1024))
                {
                }
                using (IOutputStream writeStream = await file.OpenWriteAsync(null))
                {
                }

                CloudFile file2 = share.GetRootDirectoryReference().GetFileReference("file");
                await file2.FetchAttributesAsync();
                Assert.AreEqual(1024, file2.Properties.Length);
            }
            finally
            {
                share.DeleteAsync().AsTask().Wait();
            }
        }

        /*
        [TestMethod]
        /// [Description("Create a file using file stream by specifying an access condition")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileWriteStreamOpenWithAccessConditionAsync()
        {
            CloudFileShare share = GetRandomShareReference();
            await share.CreateAsync();

            try
            {
                OperationContext context = new OperationContext();

                CloudFile existingFile = share.GetRootDirectoryReference().GetFileReference("file");
                await existingFile.CreateAsync(1024);

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file2");
                AccessCondition accessCondition = AccessCondition.GenerateIfMatchCondition(existingFile.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await file.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                file = share.GetRootDirectoryReference().GetFileReference("file3");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingFile.Properties.ETag);
                IOutputStream fileStream = await file.OpenWriteAsync(1024, accessCondition, null, context);
                fileStream.Dispose();

                file = share.GetRootDirectoryReference().GetFileReference("file4");
                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                fileStream = await file.OpenWriteAsync(1024, accessCondition, null, context);
                fileStream.Dispose();

                file = share.GetRootDirectoryReference().GetFileReference("file5");
                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(1));
                fileStream = await file.OpenWriteAsync(1024, accessCondition, null, context);
                fileStream.Dispose();

                file = share.GetRootDirectoryReference().GetFileReference("file6");
                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(-1));
                fileStream = await file.OpenWriteAsync(1024, accessCondition, null, context);
                fileStream.Dispose();

                accessCondition = AccessCondition.GenerateIfMatchCondition(existingFile.Properties.ETag);
                fileStream = await existingFile.OpenWriteAsync(1024, accessCondition, null, context);
                fileStream.Dispose();

                accessCondition = AccessCondition.GenerateIfMatchCondition(file.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingFile.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(file.Properties.ETag);
                fileStream = await existingFile.OpenWriteAsync(1024, accessCondition, null, context);
                fileStream.Dispose();

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition(existingFile.Properties.ETag);
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingFile.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNoneMatchCondition("*");
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingFile.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "FileWriteStream.Dispose with a non-met condition should fail",
                    HttpStatusCode.Conflict);

                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(-1));
                fileStream = await existingFile.OpenWriteAsync(1024, accessCondition, null, context);
                fileStream.Dispose();

                accessCondition = AccessCondition.GenerateIfModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(1));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingFile.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);

                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(1));
                fileStream = await existingFile.OpenWriteAsync(1024, accessCondition, null, context);
                fileStream.Dispose();

                accessCondition = AccessCondition.GenerateIfNotModifiedSinceCondition(existingFile.Properties.LastModified.Value.AddMinutes(-1));
                await TestHelper.ExpectedExceptionAsync(
                    async () => await existingFile.OpenWriteAsync(1024, accessCondition, null, context),
                    context,
                    "OpenWriteAsync with a non-met condition should fail",
                    HttpStatusCode.PreconditionFailed);
            }
            finally
            {
                share.DeleteAsync().AsTask().Wait();
            }
        }
        */

        [TestMethod]
        /// [Description("Upload a file using file stream and verify contents")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileWriteStreamBasicTestAsync()
        {
            byte[] buffer = GetRandomBuffer(6 * 512);

            CryptographicHash hasher = HashAlgorithmProvider.OpenAlgorithm("MD5").CreateHash();
            CloudFileShare share = GetRandomShareReference();
            share.ServiceClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;

            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.StreamWriteSizeInBytes = 8 * 512;

                using (MemoryStream wholeFile = new MemoryStream())
                {
                    FileRequestOptions options = new FileRequestOptions()
                    {
                        StoreFileContentMD5 = true,
                    };
                    using (IOutputStream writeStream = await file.OpenWriteAsync(buffer.Length * 3, null, options, null))
                    {
                        Stream fileStream = writeStream.AsStreamForWrite();

                        for (int i = 0; i < 3; i++)
                        {
                            await fileStream.WriteAsync(buffer, 0, buffer.Length);
                            await wholeFile.WriteAsync(buffer, 0, buffer.Length);
                            Assert.AreEqual(wholeFile.Position, fileStream.Position);
                            hasher.Append(buffer.AsBuffer());
                        }

                        await fileStream.FlushAsync();
                    }

                    string md5 = CryptographicBuffer.EncodeToBase64String(hasher.GetValueAndReset());
                    await file.FetchAttributesAsync();
                    Assert.AreEqual(md5, file.Properties.ContentMD5);

                    using (MemoryOutputStream downloadedFile = new MemoryOutputStream())
                    {
                        await file.DownloadToStreamAsync(downloadedFile);
                        TestHelper.AssertStreamsAreEqual(wholeFile, downloadedFile.UnderlyingStream);
                    }

                    await TestHelper.ExpectedExceptionAsync<ArgumentException>(
                        async () => await file.OpenWriteAsync(null, null, options, null),
                        "OpenWrite with StoreFileContentMD5 on an existing file should fail");

                    using (IOutputStream writeStream = await file.OpenWriteAsync(null))
                    {
                        Stream fileStream = writeStream.AsStreamForWrite();
                        fileStream.Seek(buffer.Length / 2, SeekOrigin.Begin);
                        wholeFile.Seek(buffer.Length / 2, SeekOrigin.Begin);

                        for (int i = 0; i < 2; i++)
                        {
                            fileStream.Write(buffer, 0, buffer.Length);
                            wholeFile.Write(buffer, 0, buffer.Length);
                            Assert.AreEqual(wholeFile.Position, fileStream.Position);
                        }

                        await fileStream.FlushAsync();
                    }

                    await file.FetchAttributesAsync();
                    Assert.AreEqual(md5, file.Properties.ContentMD5);

                    using (MemoryOutputStream downloadedFile = new MemoryOutputStream())
                    {
                        options.DisableContentMD5Validation = true;
                        await file.DownloadToStreamAsync(downloadedFile, null, options, null);
                        TestHelper.AssertStreamsAreEqual(wholeFile, downloadedFile.UnderlyingStream);
                    }
                }
            }
            finally
            {
                share.DeleteAsync().AsTask().Wait();
            }
        }

        [TestMethod]
        /// [Description("Upload a file using file stream and verify contents")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileWriteStreamRandomSeekTestAsync()
        {
            byte[] buffer = GetRandomBuffer(3 * 1024 * 1024);

            CloudFileShare share = GetRandomShareReference();
            share.ServiceClient.DefaultRequestOptions.ParallelOperationThreadCount = 2;
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                using (MemoryStream wholeFile = new MemoryStream())
                {
                    using (IOutputStream writeStream = await file.OpenWriteAsync(buffer.Length))
                    {
                        Stream fileStream = writeStream.AsStreamForWrite();
                        await fileStream.WriteAsync(buffer, 0, buffer.Length);
                        await wholeFile.WriteAsync(buffer, 0, buffer.Length);
                        Random random = new Random();
                        for (int i = 0; i < 10; i++)
                        {
                            int offset = random.Next(buffer.Length);
                            TestHelper.SeekRandomly(fileStream, offset);
                            await fileStream.WriteAsync(buffer, 0, buffer.Length - offset);
                            wholeFile.Seek(offset, SeekOrigin.Begin);
                            await wholeFile.WriteAsync(buffer, 0, buffer.Length - offset);
                        }
                    }

                    wholeFile.Seek(0, SeekOrigin.End);
                    await file.FetchAttributesAsync();
                    Assert.IsNull(file.Properties.ContentMD5);

                    using (MemoryOutputStream downloadedFile = new MemoryOutputStream())
                    {
                        await file.DownloadToStreamAsync(downloadedFile);
                        TestHelper.AssertStreamsAreEqual(wholeFile, downloadedFile.UnderlyingStream);
                    }
                }
            }
            finally
            {
                share.DeleteIfExistsAsync().AsTask().Wait();
            }
        }

        [TestMethod]
        /// [Description("Upload a file using file stream and verify contents")]
        [TestCategory(ComponentCategory.File)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public async Task FileWriteStreamFlushTestAsync()
        {
            byte[] buffer = GetRandomBuffer(512);

            CloudFileShare share = GetRandomShareReference();
            try
            {
                await share.CreateAsync();

                CloudFile file = share.GetRootDirectoryReference().GetFileReference("file1");
                file.StreamWriteSizeInBytes = 1024;
                using (MemoryStream wholeFile = new MemoryStream())
                {
                    FileRequestOptions options = new FileRequestOptions() { StoreFileContentMD5 = true };
                    OperationContext opContext = new OperationContext();
                    using (ICloudFileStream fileStream = await file.OpenWriteAsync(4 * 512, null, options, opContext))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            await fileStream.WriteAsync(buffer.AsBuffer());
                            await wholeFile.WriteAsync(buffer, 0, buffer.Length);
                        }

                        Assert.AreEqual(2, opContext.RequestResults.Count);

                        await fileStream.FlushAsync();

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        await fileStream.FlushAsync();

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        await fileStream.WriteAsync(buffer.AsBuffer());
                        await wholeFile.WriteAsync(buffer, 0, buffer.Length);

                        Assert.AreEqual(3, opContext.RequestResults.Count);

                        await fileStream.CommitAsync();

                        Assert.AreEqual(5, opContext.RequestResults.Count);
                    }

                    Assert.AreEqual(5, opContext.RequestResults.Count);

                    using (MemoryOutputStream downloadedFile = new MemoryOutputStream())
                    {
                        await file.DownloadToStreamAsync(downloadedFile);
                        TestHelper.AssertStreamsAreEqual(wholeFile, downloadedFile.UnderlyingStream);
                    }
                }
            }
            finally
            {
                share.DeleteIfExistsAsync().AsTask().Wait();
            }
        }
    }
}
