﻿using System;
using System.Xml;
using System.IO;
using NUnit.Framework;
using Palaso.Data;
using Palaso.IO;
using Palaso.TestUtilities;
using Palaso.WritingSystems;
using Palaso.WritingSystems.Migration.WritingSystemsLdmlV0To1Migration;
using Palaso.Xml;

namespace Palaso.Tests.WritingSystems
{
	[TestFixture]
	public class LdmlDataMapperTests
	{
		private LdmlDataMapper _adaptor;
		private WritingSystemDefinition _ws;

		[SetUp]
		public void SetUp()
		{
			_adaptor = new LdmlDataMapper();
			_ws = new WritingSystemDefinition("en", "Latn", "US", string.Empty, "eng", false);
		}

		[Test]
		public void ReadFromFile_NullFileName_Throws()
		{
			Assert.Throws<ArgumentNullException>(
				() => _adaptor.Read((string)null, _ws)
			);
		}

		[Test]
		public void ReadFromFile_NullWritingSystem_Throws()
		{
			Assert.Throws<ArgumentNullException>(
				() => _adaptor.Read("foo.ldml", null)
			);
		}

		[Test]
		public void ReadFromXmlReader_NullXmlReader_Throws()
		{
			Assert.Throws<ArgumentNullException>(
				() => _adaptor.Read((XmlReader)null, _ws)
			);
		}

		[Test]
		public void ReadFromXmlReader_NullWritingSystem_Throws()
		{
			Assert.Throws<ArgumentNullException>(
				() => _adaptor.Read(XmlReader.Create(new StringReader("<ldml/>")), null)
			);
		}

		[Test]
		public void WriteToFile_NullFileName_Throws()
		{
			Assert.Throws<ArgumentNullException>(
				() => _adaptor.Write((string)null, _ws, null)
			);
		}

		[Test]
		public void WriteToFile_NullWritingSystem_Throws()
		{
			Assert.Throws<ArgumentNullException>(
				() => _adaptor.Write("foo.ldml", null, null)
			);
		}

		[Test]
		public void WriteToXmlWriter_NullXmlReader_Throws()
		{
			Assert.Throws<ArgumentNullException>(
				() => _adaptor.Write((XmlWriter)null, _ws, null)
			);
		}

		[Test]
		public void WriteSetsRequiresValidTagToTrue()
		{
			var ws = new WritingSystemDefinition();
			ws.RequiresValidTag = false;
			ws.Language = "Kalaba";
			var sw = new StringWriter();
			var writer = XmlWriter.Create(sw, CanonicalXmlSettings.CreateXmlWriterSettings());
			Assert.Throws(typeof(ValidationException), () => _adaptor.Write(writer, ws, null));
		}

		[Test]
		public void WriteToXmlWriter_NullWritingSystem_Throws()
		{
			Assert.Throws<ArgumentNullException>(
				() => _adaptor.Write(XmlWriter.Create(new MemoryStream()), null, null)
			);
		}

		[Test]
		public void ExistingUnusedLdml_Write_PreservesData()
		{
			var sw = new StringWriter();
			var ws = new WritingSystemDefinition("en");
			var writer = XmlWriter.Create(sw, CanonicalXmlSettings.CreateXmlWriterSettings());
			_adaptor.Write(writer, ws, XmlReader.Create(new StringReader("<ldml><!--Comment--><dates/><special>hey</special></ldml>")));
			writer.Close();
			AssertThatXmlIn.String(sw.ToString()).HasAtLeastOneMatchForXpath("/ldml/special[text()=\"hey\"]");
		}

		[Test]
		public void RoundtripSimpleCustomSortRules_WS33715()
		{
			var ldmlAdaptor = new LdmlDataMapper();

			const string sortRules = "(A̍ a̍)";
			var wsWithSimpleCustomSortRules = new WritingSystemDefinition();
			wsWithSimpleCustomSortRules.SortUsing = WritingSystemDefinition.SortRulesType.CustomSimple;
			wsWithSimpleCustomSortRules.SortRules = sortRules;

			var wsFromLdml = new WritingSystemDefinition();
			using (var tempFile = new TempFile())
			{
				ldmlAdaptor.Write(tempFile.Path, wsWithSimpleCustomSortRules, null);
				ldmlAdaptor.Read(tempFile.Path, wsFromLdml);
			}

			Assert.AreEqual(sortRules, wsFromLdml.SortRules);
		}


		[Test]
		//WS-33992
		public void Read_LdmlContainsEmptyCollationElement_SortUsingIsSetToSameAsIfNoCollationElementExisted()
		{
			const string ldmlWithEmptyCollationElement = "<ldml><!--Comment--><identity><version number=\"\" /><generation date=\"0001-01-01T00:00:00\" /><language type=\"qaa\" /></identity><dates /><collations><collation></collation></collations><special xmlns:palaso=\"urn://palaso.org/ldmlExtensions/v1\" ><palaso:version value=\"2\" /></special></ldml>";
			const string ldmlwithNoCollationElement = "<ldml><!--Comment--><identity><version number=\"\" /><generation date=\"0001-01-01T00:00:00\" /><language type=\"qaa\" /></identity><dates /><collations/><special xmlns:palaso=\"urn://palaso.org/ldmlExtensions/v1\" ><palaso:version value=\"2\" /></special></ldml>";

			string pathToLdmlWithEmptyCollationElement = Path.GetTempFileName();
			File.WriteAllText(pathToLdmlWithEmptyCollationElement, ldmlWithEmptyCollationElement);
			string pathToLdmlWithNoCollationElement = Path.GetTempFileName();
			File.WriteAllText(pathToLdmlWithNoCollationElement, ldmlwithNoCollationElement);


			var adaptor = new LdmlDataMapper();
			var wsFromEmptyCollationElement = new WritingSystemDefinition();
			adaptor.Read(pathToLdmlWithEmptyCollationElement, wsFromEmptyCollationElement);
			var wsFromNoCollationElement = new WritingSystemDefinition();
			adaptor.Read(pathToLdmlWithNoCollationElement, wsFromNoCollationElement);

			Assert.AreEqual(wsFromNoCollationElement.SortUsing, wsFromEmptyCollationElement.SortUsing);
		}


		[Test]
		public void Read_LdmlContainsOnlyPrivateUse_IsoAndprivateUseSetCorrectly()
		{
			const string ldmlWithOnlyPrivateUse = "<ldml><identity><version number=\"\" /><language type=\"\" /><variant type=\"x-private-use\" /></identity><special xmlns:palaso=\"urn://palaso.org/ldmlExtensions/v1\" ><palaso:version value=\"2\" /></special></ldml>";


			string pathToLdmlWithEmptyCollationElement = Path.GetTempFileName();
			File.WriteAllText(pathToLdmlWithEmptyCollationElement, ldmlWithOnlyPrivateUse);

			var adaptor = new LdmlDataMapper();
			var wsFromLdml = new WritingSystemDefinition();
			adaptor.Read(pathToLdmlWithEmptyCollationElement, wsFromLdml);
			var ws = new WritingSystemDefinition();
			adaptor.Read(pathToLdmlWithEmptyCollationElement, ws);
			Assert.That(wsFromLdml.Language, Is.EqualTo(String.Empty));
			Assert.That(wsFromLdml.Variant, Is.EqualTo("x-private-use"));
		}

		[Test]
		public void Write_LdmlIsNicelyFormatted()
		{
			string expectedFileContent =
#region filecontent
@"<?xml version='1.0' encoding='utf-8'?>
<ldml>
	<identity>
		<version
			number='' />
		<generation
			date='0001-01-01T00:00:00' />
		<language
			type='en' />
		<script
			type='Zxxx' />
		<territory
			type='US' />
		<variant
			type='x-audio' />
	</identity>
	<collations />
	<special xmlns:palaso='urn://palaso.org/ldmlExtensions/v1'>
		<palaso:abbreviation
			value='en' />
		<palaso:languageName
			value='English' />
		<palaso:version
			value='2' />
	</special>
</ldml>".Replace("'", "\"");
#endregion
			using (var file = new TempFile())
			{
				//Create an ldml fiel to read
				var adaptor = new LdmlDataMapper();
				var ws = WritingSystemDefinition.Parse("en-Zxxx-x-audio");
				adaptor.Write(file.Path, ws, null);

				//change the read writing system and write it out again
				var ws2 = new WritingSystemDefinition();
				adaptor.Read(file.Path, ws2);
				ws2.Region = "US";
				adaptor.Write(file.Path, ws2, new MemoryStream(File.ReadAllBytes(file.Path)));

				Assert.That(File.ReadAllText(file.Path), Is.EqualTo(expectedFileContent));
			}
		}

		[Test]
		public void Write_WritingSystemWasloadedFromLdmlThatContainedLayoutInfo_LayoutInfoIsOnlyWrittenOnce()
		{
			using (var file = new TempFile())
			{
				//create an ldml file to read that contains layout info
				var adaptor = new LdmlDataMapper();
				var ws = WritingSystemDefinition.Parse("en-Zxxx-x-audio");
				ws.RightToLeftScript = true;
				adaptor.Write(file.Path, ws, null);

				//read the file and write it out unchanged
				var ws2 = new WritingSystemDefinition();
				adaptor.Read(file.Path, ws2);
				adaptor.Write(file.Path, ws2, new MemoryStream(File.ReadAllBytes(file.Path)));

				AssertThatXmlIn.File(file.Path).HasNoMatchForXpath("/ldml/layout[2]");
			}
		}

		[Test]
		public void ReadNormalLdmlMissingVersion1Element_Throws()
		{
			using (var version1Ldml = new TempFile())
			{
				WriteVersion0Ldml("en", "", "", "", version1Ldml);
				var wsV1 = new WritingSystemDefinition();
				var adaptor = new LdmlDataMapper();
				Assert.Throws<FormatException>(() => adaptor.Read(version1Ldml.Path, wsV1));
			}
		}

		[Test]
		public void Read_NonDescriptLdml_WritingSystemIdIsSameAsRfc5646Tag()
		{
			using (var file = new TempFile())
			{
				WriteVersion1Ldml("en", "Zxxx", "US", "1901-x-audio", file);
				var ws = new WritingSystemDefinition();
				new LdmlDataMapper().Read(file.Path, ws);
				Assert.That(ws.Id, Is.EqualTo("en-Zxxx-US-1901-x-audio"));
			}
		}

		private static void AssertThatRfcTagComponentsOnWritingSystemAreEqualTo(WritingSystemDefinition ws, string language, string script, string territory, string variant)
		{
			Assert.That(ws.Language, Is.EqualTo(language));
			Assert.That(ws.Script, Is.EqualTo(script));
			Assert.That(ws.Region, Is.EqualTo(territory));
			Assert.That(ws.Variant, Is.EqualTo(variant));
		}

		private static void WriteVersion0Ldml(string language, string script, string territory, string variant, TempFile file)
		{
			//using a writing system V0 here because the real writing system can't cope with the way
			//flex encodes private-use language and shouldn't. But using a writing system + ldml adaptor
			//is the quickest way to generate ldml so I'm using it here.
			var ws = new WritingSystemDefinitionV0
						 {ISO639 = language, Script = script, Region = territory, Variant = variant};
			new LdmlAdaptorV0().Write(file.Path, ws, null);
		}

		private static void WriteVersion1Ldml(string language, string script, string territory, string variant, TempFile file)
		{
			//using a writing system V0 here because the real writing system can't cope with the way
			//flex encodes private-use language and shouldn't. But using a writing system + ldml adaptor
			//is the quickest way to generate ldml so I'm using it here.
			var ws = new WritingSystemDefinition { Language = language, Script = script, Region = territory, Variant = variant };
			new LdmlDataMapper().Write(file.Path, ws, null);
		}

		private static void AssertThatLdmlMatches(string language, string script, string territory, string variant, TempFile file)
		{
			AssertThatIdentityElementIsCorrectForContent("language", language, file);
			AssertThatIdentityElementIsCorrectForContent("script", script, file);
			AssertThatIdentityElementIsCorrectForContent("territory", territory, file);
			AssertThatIdentityElementIsCorrectForContent("variant", variant, file);
		}

		private static void AssertThatIdentityElementIsCorrectForContent(string element, string content, TempFile file)
		{
			if (String.IsNullOrEmpty(content) && element != "language")
			{
				AssertThatXmlIn.File(file.Path).HasNoMatchForXpath(String.Format("/ldml/identity/{0}", element));
				return;
			}
			AssertThatXmlIn.File(file.Path).HasAtLeastOneMatchForXpath(String.Format("/ldml/identity/{0}[@type='{1}']", element, content));
		}
	}
}
