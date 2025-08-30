namespace Wfc.Core.Localization.Test;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using GodotTestDriver;
using LightMock.Generator;
using LightMoq;
using NUnit.Framework;
using Shouldly;
using Wfc.Entities.World.Player;
using Wfc.Utils;

public class StringUtilsTest(Node testScene) : TestClass(testScene) {

  [TestCase("TestString", ExpectedResult = "test_string")]
  [TestCase("testString", ExpectedResult = "test_string")]
  [TestCase("Test", ExpectedResult = "test")]
  [TestCase("XMLHttpRequest", ExpectedResult = "x_m_l_http_request")]
  [TestCase("", ExpectedResult = "")]
  [TestCase("already_snake_case", ExpectedResult = "already_snake_case")]
  [TestCase("snake_Case_Mix", ExpectedResult = "snake__case__mix")]
  public string ToSnakeCase_ShouldConvertCorrectly(string input) {
    return StringUtils.ToSnakeCase(input);
  }
}
