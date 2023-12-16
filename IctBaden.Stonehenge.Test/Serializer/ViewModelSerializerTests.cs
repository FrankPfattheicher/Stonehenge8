using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using IctBaden.Stonehenge.Hosting;
using IctBaden.Stonehenge.ViewModel;
using Microsoft.Extensions.Logging;
using Xunit;

// ReSharper disable LocalFunctionCanBeMadeStatic
// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace IctBaden.Stonehenge.Test.Serializer
{
    public class ViewModelSerializerTests
    {
        private readonly ILogger _logger = StonehengeLogger.DefaultLogger;
        
        [Fact]
        public void SimpleClassSerializationShouldWork()
        {
            var model = new SimpleClass
            {
                Integer = 7,
                FloatingPoint = 1.23,
                Text = "test",
                PrivateText = "invisible",
                Timestamp = new DateTime(2016, 11, 11, 12, 13, 14, DateTimeKind.Utc),
                Wieviel = TestEnum.Fumpf
            };

            var json = Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(model));
            
            var obj = JsonSerializer.Deserialize<object>(json);
            Assert.NotNull(obj);

            // public properties - not NULL
            Assert.Contains("Integer", json);
            Assert.Contains("7", json);

            Assert.Contains("Boolean", json);
            Assert.Contains("false", json);

            Assert.Contains("FloatingPoint", json);
            Assert.Contains("1.23", json);

            Assert.Contains("Text", json);
            Assert.Contains("test", json);

            Assert.Contains("Timestamp", json);
            Assert.Contains("2016-11-11T12:13:14Z", json);

            Assert.Contains("Wieviel", json);
            Assert.Contains("5", json);

            // private fields
            Assert.DoesNotContain("PrivateText", json);
            Assert.DoesNotContain("invisible", json);
        }

        [Fact]
        public void StringsIncludingNewlineShouldBeEscaped()
        {
            var model = new SimpleClass
            {
                Text = "line1" + Environment.NewLine + "line2"
            };

            var json = Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(model));

            var obj = JsonSerializer.Deserialize<object>(json);
            Assert.NotNull(obj);

            Assert.Contains("\\n", json);
        }

        [Fact]
        public void SerializerShouldRespectAttributes()
        {
            //TODO   
        }

        [Fact]
        public void SerializerShouldRespectCustomSerializers()
        {
            //TODO   
        }

        [Fact]
        public void NestedClassesSerializationShouldWork()
        {
            var simple = new SimpleClass
            {
                Integer = 7,
                FloatingPoint = 1.23,
                Text = "test",
                PrivateText = "invisible",
                Timestamp = new DateTime(2016, 11, 11, 12, 13, 14, DateTimeKind.Utc),
                Timeoffset = new DateTimeOffset(2016, 11, 11, 12, 13, 14, TimeSpan.Zero),
                Wieviel = TestEnum.Fumpf
            };

            var model = new NestedClass
            {
                //Name = "outer",
                Nested = new List<NestedClass2>
                {
                    new NestedClass2
                    {
                        NestedSimple = new[] {simple, simple, simple}
                    }
                }
            };


            var json = Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(model));

            var obj = JsonSerializer.Deserialize<SimpleClass>(json);
            Assert.NotNull(obj);

            Assert.StartsWith("{", json);
            Assert.EndsWith("}", json);
        }

        [Fact]
        public void DeserializationOfDoubleShouldWorkWithCurrentAndInternationalFormat()
        {
            var json = "{ \"FloatingPoint\": \"5\" }";
            var obj = ViewModelProvider.DeserializePropertyValue(_logger, json, typeof(SimpleClass)) as SimpleClass;
            Assert.NotNull(obj);
            Assert.Equal(5.0, obj.FloatingPoint);

            var value = 5.3.ToString(CultureInfo.InvariantCulture);
            json = $"{{ \"FloatingPoint\": \"{value}\" }}";
            obj = ViewModelProvider.DeserializePropertyValue(_logger, json, typeof(SimpleClass)) as SimpleClass;
            Assert.NotNull(obj);
            Assert.Equal(5.3, obj.FloatingPoint);
            
            value = 5.75.ToString(CultureInfo.CurrentCulture);
            json = $"{{ \"FloatingPoint\": \"{value}\" }}";
            obj = ViewModelProvider.DeserializePropertyValue(_logger, json, typeof(SimpleClass)) as SimpleClass;
            Assert.NotNull(obj);
            Assert.Equal(5.75, obj.FloatingPoint);
        }
        

        [Fact]
        public void EnumPropertySerializedAsStringDeserializationShouldWork()
        {
            var json = "{ \"Wieviel\": \"5\" }";
            var obj = ViewModelProvider.DeserializePropertyValue(_logger, json, typeof(SimpleClass));
            Assert.NotNull(obj);
        }
        

        [Fact]
        public void HierarchicalClassesSerializationShouldWork()
        {
            HierarchicalClass NewHierarchicalClass(string name, int depth)
            {
                return new HierarchicalClass
                {
                    Name = name,
                    Children = depth > 0
                        ? Enumerable.Range(1, 10)
                            .Select(ix => NewHierarchicalClass($"child {depth} {ix}", depth - 1))
                            .ToList()
                        : new List<HierarchicalClass>()
                };
            }

            var hierarchy = NewHierarchicalClass("Root", 3);
            
            var watch = new Stopwatch();
            watch.Start();
            
            var json = Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(hierarchy));

            watch.Stop();
            _logger.LogTrace($"HierarchicalClassesSerialization: {watch.ElapsedMilliseconds}ms");
            
            var obj = JsonSerializer.Deserialize<object>(json);
            Assert.NotNull(obj);

            Assert.StartsWith("{", json);
            Assert.EndsWith("}", json);
        }

        [Fact]
        public void DictionaryStringObjectSerializationShouldBeDoneAsObjects()
        {
            var dt = new DateTime(2020, 02, 12, 17, 37, 44, DateTimeKind.Utc);
            var dto = new DateTimeOffset(2016, 11, 11, 12, 13, 14, TimeSpan.Zero);

            var dict = new Dictionary<string, object>
            {
                { "Integer", 7 },
                { "FloatingPoint", 1.23 },
                { "Text", "test" },
                { "Timestamp", dt },
                { "Timeoffset", dto },
                { "Wieviel", 5 }
            };
            
            var json = Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(dict));
 
            var obj = JsonSerializer.Deserialize<JsonObject>(json);
            Assert.NotNull(obj);

            Assert.Equal(7, obj["Integer"]?.GetValue<int>());
            Assert.Equal(1.23, obj["FloatingPoint"]?.GetValue<double>());
            Assert.Equal("test", obj["Text"]?.GetValue<string>());
            Assert.Equal(dt, obj["Timestamp"]?.GetValue<DateTime>());
            Assert.Equal(dto, obj["Timeoffset"]?.GetValue<DateTimeOffset>());
            Assert.Equal((int)TestEnum.Fumpf, obj["Wieviel"]?.GetValue<int>());
        }

        [Fact]
        public void SimpleSerializationShouldDeserializeAlso()
        {
            var dt = new DateTime(2016, 11, 11, 12, 13, 14, DateTimeKind.Utc);
            var dto = new DateTimeOffset(2016, 11, 11, 12, 13, 14, TimeSpan.Zero);

            var simple = new SimpleClass
            {
                Integer = 7,
                FloatingPoint = 1.23,
                Text = "test",
                PrivateText = "invisible",
                Timestamp = dt,
                Timeoffset = dto,
                Wieviel = TestEnum.Fumpf
            };
           
            var json = Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(simple));
 
            var obj = JsonSerializer.Deserialize<SimpleClass>(json);
            Assert.NotNull(obj);

            Assert.Equal(7, obj.Integer);
            Assert.Equal(1.23, obj.FloatingPoint);
            Assert.Equal("test", obj.Text);
            Assert.Equal(dt, obj.Timestamp);
            Assert.Equal(dto, obj.Timeoffset);
            Assert.Equal(TestEnum.Fumpf, obj.Wieviel);
        }

        [Fact]
        public void DeserializationShouldDeserializeClass()
        {
            var json = "{\n  \"Id\": \"52950eb67afd464cb3e3cf6b8ad09ebf\",\n  \"RequestTimestamp\": \"0001-01-01T00:00:00\",\n  \"Name\": \"tuzu\",\n  \"Gender\": 0,\n  \"Birthdate\": \"2021-05-16T09:23:00.2718243\\u002B02:00\",\n  \"Assignment\": 1,\n  \"Enrollment\": \"2022-09-01T00:00:00\"\n}";
            var obj = ViewModelProvider.DeserializePropertyValue(_logger, json, typeof(object));
            Assert.NotNull(obj);
        }

        [Fact]
        public void DeserializationShouldDeserializeArrayOfClassToList()
        {
            var dt = new DateTime(2022, 04, 13, 12, 11, 10, DateTimeKind.Utc);
            var dto = new DateTimeOffset(2022, 04, 13, 12, 11, 10, TimeSpan.Zero);
            var simple = new SimpleClass
            {
                Integer = 7,
                FloatingPoint = 1.23,
                Text = "test",
                PrivateText = "invisible",
                Timestamp = dt,
                Timeoffset = dto,
                Wieviel = TestEnum.Fumpf
            };
            var json = Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(simple));
            json = $"[ {json}, {json} ]";
            var listSimpleClass = ViewModelProvider
                .DeserializePropertyValue(_logger, json, typeof(List<SimpleClass>))
                as List<SimpleClass>;
            Assert.NotNull(listSimpleClass);
            
            foreach (var obj in listSimpleClass)
            {
                Assert.Equal(7, obj.Integer);
                Assert.Equal(1.23, obj.FloatingPoint);
                Assert.Equal("test", obj.Text);
                Assert.Equal(dt, obj.Timestamp.ToUniversalTime());
                Assert.Equal(dto, obj.Timeoffset);
                Assert.Equal(TestEnum.Fumpf, obj.Wieviel);
            }
        }

        [Fact]
        public void DeserializationShouldDeserializeArrayOfClassToArray()
        {
            var dt = new DateTime(2022, 04, 13, 12, 11, 10, DateTimeKind.Utc);
            var dto = new DateTimeOffset(2022, 04, 13, 12, 11, 10, TimeSpan.Zero);
            var simple = new SimpleClass
            {
                Integer = 7,
                FloatingPoint = 1.23,
                Text = "test",
                PrivateText = "invisible",
                Timestamp = dt,
                Timeoffset = dto,
                Wieviel = TestEnum.Fumpf
            };
            var json = Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(simple));
            json = $"[ {json}, {json} ]";
            var arraySimpleClass = ViewModelProvider
                .DeserializePropertyValue(_logger, json, typeof(SimpleClass[]))
                as SimpleClass[];
            Assert.NotNull(arraySimpleClass);
            
            foreach (var obj in arraySimpleClass)
            {
                Assert.Equal(7, obj.Integer);
                Assert.Equal(1.23, obj.FloatingPoint);
                Assert.Equal("test", obj.Text);
                Assert.Equal(dt, obj.Timestamp.ToUniversalTime());
                Assert.Equal(dto, obj.Timeoffset);
                Assert.Equal(TestEnum.Fumpf, obj.Wieviel);
            }
        }
        
    }
}