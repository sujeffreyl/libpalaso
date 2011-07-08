﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Palaso.IO;
using Palaso.TestUtilities;
using Palaso.WritingSystems;

namespace Palaso.Tests.WritingSystems
{
	[TestFixture]
	public class WritingSystemChangeLogDataMapperTests
	{
		[Test]
		public void Write_NullChangeLog_Throws()
		{
			var dataMapper = new WritingSystemChangeLogDataMapper("whatever");
			Assert.Throws<ArgumentNullException>(
				() => dataMapper.Write(null)
			);
		}

		[Test]
		public void Read_SampleLogFile_PopulatesModel()
		{
			using (var e = new TestEnvironment())
			{
				var log = new WritingSystemChangeLog(new WritingSystemChangeLogDataMapper(e.GetSampleLogFilePath()));
				Assert.That(log.HasChangeFor("aaa"));
				Assert.That(log.GetChangeFor("aaa"), Is.EqualTo("ccc"));
			}
		}

		[Test]
		public void Write_NewEmptyFile_WritesModelToLogFile()
		{
			using (var e = new TestEnvironment())
			{
				string tempFilePath = Path.Combine(e.tempFolder.Path, "testchangelog.xml");
				var log = new WritingSystemChangeLog(new WritingSystemChangeLogDataMapper(tempFilePath));
				log.LogChange("aab", "bba");
				log.LogAdd("aab");
				log.LogDelete("aab");
				AssertThatXmlIn.File(tempFilePath).HasAtLeastOneMatchForXpath("/WritingSystemChangeLog/Changes/Change/From[text()='aab']");
				AssertThatXmlIn.File(tempFilePath).HasAtLeastOneMatchForXpath("/WritingSystemChangeLog/Changes/Add/Id[text()='aab']");
				AssertThatXmlIn.File(tempFilePath).HasAtLeastOneMatchForXpath("/WritingSystemChangeLog/Changes/Delete/Id[text()='aab']");
			}
		}

		public class TestEnvironment : IDisposable
		{
			public TemporaryFolder tempFolder = new TemporaryFolder("writingSystemChangeLogTests");

			public TestEnvironment()
			{
				TempFile file = new TempFile();
			}

			public string GetSampleLogFilePath()
			{
				string contents = String.Format(@"<WritingSystemChangeLog Version='1'>
<Changes>
	<Change Producer='WeSay' ProducerVersion='1.1' TimeStamp='1994-11-05T13:15:30Z'>
		<From>aaa</From>
		<To>ccc</To>
	</Change>
	<Change Producer='WeSay' ProducerVersion='1.1' TimeStamp='1994-11-06T13:15:30Z'>
		<From>bbb</From>
		<To>ddd</To>
	</Change>
	<Change Producer='WeSay' ProducerVersion='1.1' TimeStamp='1994-11-06T13:15:30Z'>
		<Delete>eee</Delete>
	</Change>
	<Change Producer='WeSay' ProducerVersion='1.1' TimeStamp='1994-11-06T13:15:30Z'>
		<Add>fff</Add>
	</Change>
</Changes>
</WritingSystemChangeLog>
").Replace("'", "\"");
				var tempFile = new TempFile(contents);
				return tempFile.Path;
			}

			public WritingSystemChangeLog Log { get; set; }
			public void Dispose()
			{
			}

			public WritingSystemChangeLog GetSampleWritingSystemChangeLog()
			{
				var log = new WritingSystemChangeLog();
				log.LogChange("aab", "bba");
				log.LogChange("ccc", "ddd");
				return log;
			}
		}
	}
}