﻿using NUnit.Framework;

namespace ImTools.UnitTests
{
    [TestFixture]
    public class OptTests
    {
        [Test]
        public void Test()
        {
            var o1 = new DataObject(100, "Value1", new DataObject2(10m, "CleanPrice"));
            var o2 = o1.With(500, obj: o1.Object.With(500));
            Assert.That(o2.Id, Is.EqualTo(500));
            Assert.That(o2.Object.Price, Is.EqualTo(500));
        }

        public class DataObject
        {
            public int Id { get; private set; }
            public string Value { get; private set; }
            public DataObject2 Object { get; private set; }

            public DataObject(int id, string value, DataObject2 o)
            {
                Id = id;
                Value = value;
                Object = o;
            }

            private DataObject() { }

            public DataObject With(Opt<int> id = default(Opt<int>), Opt<string> value = default(Opt<string>), Opt<DataObject2> obj = default(Opt<DataObject2>))
            {
                return new DataObject
                {
                    Id = id.OrDefault(Id),
                    Value = value.OrDefault(Value),
                    Object = obj.OrDefault(Object)
                };
            }
        }

        public class DataObject2
        {
            public decimal Price { get; private set; }
            public string PriceType { get; private set; }

            public DataObject2(decimal price, string priceType)
            {
                Price = price;
                PriceType = priceType;
            }

            private DataObject2()
            {
            }

            public DataObject2 With(Opt<decimal> price = default(Opt<decimal>), Opt<string> priceType = default(Opt<string>))
            {
                return new DataObject2
                {
                    Price = price.OrDefault(Price),
                    PriceType = priceType.OrDefault(PriceType)
                };
            }
        }
    }
}
