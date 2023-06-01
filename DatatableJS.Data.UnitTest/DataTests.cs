using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace DatatableJS.Data.UnitTest
{
    [TestFixture]
    public class Tests
    {
        IQueryable<Person> _list;
        DataRequest _request;

        [SetUp]
        public void Setup()
        {
            _list = new List<Person>
            {
                new Person { Name = "Jon", Age = 1, Address= new Address{Street="CC" }},
                new Person { Name = "Arya", Age = 1, Address= new Address{Street="BB" } },
                new Person { Name = "Arya", Age = 2, Address= new Address{Street="AA" } }
            }.AsQueryable();

            var _columns = new List<Column> {
                new Column {data = "Name", name = "Name", orderable = true, searchable = true, search = new Search()},
                new Column {data = "Age", name = "Age", orderable = true, searchable = true, search = new Search()},
                new Column {data = "Address", name = "Address", orderable = true, searchable = true, search = new Search()}
            };

            _request = new DataRequest { columns = _columns, draw = 1, length = 10 };
        }

        [Test]
        public void ToDataResult_WhenFilterComplexObject_ReturnsList()
        {
            _request.filters.Add(new Filter { Field = "Address.Street", Operand = Operand.Contains, Value = "AA" });
            var result = _list.ToDataResult(_request);

            Assert.That(result.data.Count, Is.EqualTo(1));
        }

        [Test]
        public void ToDataResult_IgnoreCase_ReturnsList()
        {
            _request.filters.Add(new Filter { Field = "Name", Operand = Operand.Contains, Value = "jOn", IgnoreCase=true });
            var result = _list.ToDataResult(_request);

            Assert.That(result.data.Count, Is.EqualTo(1));
        }

        [Test]
        public void ToDataResult_WhenSearchomplexObject_ReturnsList()
        {
            _request.columns[2].search.value =  "AA" ;
            var result = _list.ToDataResult(_request);

            Assert.That(result.data.Count, Is.EqualTo(1));
        }

        [Test]
        public void ToDataResult_WhenFilterWithNameDoesNotContain_ReturnsListCountZero()
        {
            _request.filters.Add(new Filter { Field = "Name", Operand = Operand.Contains, Value = "Jon" });
            var result = _list.ToDataResult(_request);

            Assert.That(result.data.Count, Is.EqualTo(1));
        }

        [Test]
        public void ToDataResult_WhenSortingByNameAndAge()
        {
            _request.order.Add(new Order
            {
                column = 0,
                dir = "asc"
            });
            _request.order.Add(new Order
            {
                column = 1,
                dir = "asc"
            });
            var result = _list.ToDataResult(_request);

            Assert.That(result.data[0].Name, Is.EqualTo("Arya"));
            Assert.That(result.data[0].Age, Is.EqualTo(1));
            Assert.That(result.data[1].Name, Is.EqualTo("Arya"));
            Assert.That(result.data[1].Age, Is.EqualTo(2));
        }
    }


    internal class StringToObjectConverter<T> : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            string stringValue = value as string;

            if (stringValue != null)
            {
                return Activator.CreateInstance(typeof(T), stringValue);
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor) && value is T)
            {
                T obj = (T)value;

                ConstructorInfo ctor = typeof(T).GetConstructor(new Type[] { typeof(string) });

                if (ctor != null)
                {
                    return new InstanceDescriptor(ctor, new object[] { obj.ToString() });
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    //[TypeConverter(typeof(StringToObjectConverter<Address>))]
    public class Address 
    {
        public Address() { }
        public Address(string street)
        {
            Street = street;
        }

        public string Street { get; set; }


        public static explicit operator Address(string street) => new Address(street);

        public static bool operator !=(Address a, Address b)
        {
            return !(a == b);
        }

        public static bool operator==(Address a, Address b)
        {
            return a?.Street == b?.Street;
        }
    }

    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }

        public Address Address { get; set; }
    }
}