using System;
using System.Collections.Generic;
using System.Data;
using NUnit.Framework;
using NServiceKit.DataAnnotations;
using NServiceKit.OrmLite.Firebird;

namespace NServiceKit.OrmLite.FirebirdTests
{
    /// <summary>A schema use case.</summary>
    [TestFixture]
    public class SchemaUseCase
    {
        /// <summary>An user.</summary>
        [Alias("Users")]
        [Schema("Security")]
        public class User
        {
            /// <summary>Gets or sets the identifier.</summary>
            /// <value>The identifier.</value>
            [AutoIncrement]
            public int Id { get; set; }

            /// <summary>Gets or sets the name.</summary>
            /// <value>The name.</value>
            [DataAnnotations.Index]
            public string Name { get; set; }

            /// <summary>Gets or sets the created date.</summary>
            /// <value>The created date.</value>
            public DateTime CreatedDate { get; set; }
        }

        /// <summary>Can create tables with schema in sqlite.</summary>
        [Test]
        public void Can_Create_Tables_With_Schema_In_Sqlite()
        {
            OrmLiteConfig.DialectProvider = new FirebirdOrmLiteDialectProvider();

            using (IDbConnection db = "User=SYSDBA;Password=masterkey;Database=ormlite-tests.fdb;DataSource=localhost;Dialect=3;charset=ISO8859_1;".OpenDbConnection())
            {
                db.CreateTable<User>(true);

                var schema  =  new Schema(){Connection= db};
				var table = schema.GetTable("Security_Users".ToUpper());                    
				
                //sqlite dialect should just concatenate the schema and table name to create a unique table name
                Assert.That( ! (table == null) );
            }
        }

        /// <summary>Can perform crud operations on table with schema.</summary>
        [Test]
        public void Can_Perform_CRUD_Operations_On_Table_With_Schema()
        {
            var dbFactory = new OrmLiteConnectionFactory(
                "User=SYSDBA;Password=masterkey;Database=ormlite-tests.fdb;DataSource=localhost;Dialect=3;charset=ISO8859_1;",
                FirebirdOrmLiteDialectProvider.Instance);
            using (IDbConnection db = dbFactory.OpenDbConnection())
            {
                
                db.CreateTable<User>(true);

                db.Insert(new User { Id = 1, Name = "A", CreatedDate = DateTime.Now });
                db.Insert(new User { Id = 2, Name = "B", CreatedDate = DateTime.Now });
                db.Insert(new User { Id = 3, Name = "B", CreatedDate = DateTime.Now });

                var lastInsertId = db.GetLastInsertId();
                Assert.That(lastInsertId, Is.GreaterThan(0));

                var rowsB = db.Select<User>("Name = {0}", "B");
                Assert.That(rowsB, Has.Count.EqualTo(2));

                var rowIds = rowsB.ConvertAll(x => x.Id);
                Assert.That(rowIds, Is.EquivalentTo(new List<long> { 2, 3 }));

                rowsB.ForEach(x => db.Delete(x));

                rowsB = db.Select<User>("Name = {0}", "B");
                Assert.That(rowsB, Has.Count.EqualTo(0));

                var rowsLeft = db.Select<User>();
                Assert.That(rowsLeft, Has.Count.EqualTo(1));

                Assert.That(rowsLeft[0].Name, Is.EqualTo("A"));
            }
        }
    }
}
