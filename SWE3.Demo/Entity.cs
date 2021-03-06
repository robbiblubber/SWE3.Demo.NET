using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace SWE3.Demo
{
    /// <summary>This class holds entity metadata.</summary>
    public sealed class Entity
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // constructors                                                                                                     //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        /// <summary>Creates a new instance of this class.</summary>
        /// <param name="t">Type.</param>
        public Entity(Type t)
        {
            entityAttribute tattr = (entityAttribute) t.GetCustomAttribute(typeof(entityAttribute));
            if((tattr == null) || (string.IsNullOrWhiteSpace(tattr.TableName)))
            {
                TableName = t.Name.ToUpper();
            }
            else { TableName = tattr.TableName; }

            EntityType = t;
            List<Field> fields = new List<Field>();
            List<Field> pks = new List<Field>();

            foreach(PropertyInfo i in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if((ignoreAttribute) i.GetCustomAttribute(typeof(ignoreAttribute)) != null) continue;

                Field field = new Field(this);

                fieldAttribute fattr = (fieldAttribute) i.GetCustomAttribute(typeof(fieldAttribute));

                if(fattr != null)
                {
                    if(fattr is pkAttribute) 
                    { 
                        pks.Add(field);
                        field.IsPrimaryKey = true;
                    }

                    field.ColumnName = (fattr?.ColumnName ?? i.Name);
                    field.ColumnType = (fattr?.ColumnType ?? i.PropertyType);

                    field.IsNullable = (fattr.Nullable);

                    if(field.IsForeignKey = (fattr is fkAttribute))
                    {
                        field.IsExternal = typeof(IEnumerable).IsAssignableFrom(i.PropertyType);
                        field.AssignmentTable  = ((fkAttribute) fattr).AssignmentTable;
                        field.RemoteColumnName = ((fkAttribute) fattr).RemoteColumnName;
                    }
                }
                else
                {
                    if((i.GetGetMethod() == null) || (!i.GetGetMethod().IsPublic)) continue;

                    field.ColumnName = i.Name;
                    field.ColumnType = i.PropertyType;
                }                
                field.FieldMember = i;

                fields.Add(field);
            }

            Fields = fields.ToArray();
            Externals = fields.Where(m => m.IsExternal).ToArray();
            Internals = fields.Where(m => (!m.IsExternal)).ToArray();

            PrimaryKeys = pks.ToArray();
        }



        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public properties                                                                                                //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        /// <summary>Gets the primary keys.</summary>
        public Field[] PrimaryKeys
        {
            get; internal set;
        }


        /// <summary>Gets or sets the entity type.</summary>
        public Type EntityType
        {
            get; set;
        }


        /// <summary>Gets the table name.</summary>
        public string TableName
        {
            get; private set;
        }


        /// <summary>Gets the entity fields.</summary>
        public Field[] Fields
        {
            get; private set;
        }


        /// <summary>Gets external fields.</summary>
        /// <remarks>External fields are referenced fields that do not belong to the underlying table.</remarks>
        public Field[] Externals
        {
            get; set;
        }


        /// <summary>Gets internal fields.</summary>
        public Field[] Internals
        {
            get; set;
        }



        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public methods                                                                                                   //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        /// <summary>Gets the entity SQL.</summary>
        /// <param name="prefix">Prefix.</param>
        /// <returns>SQL string.</returns>
        public string GetSQL(string prefix = null)
        {
            if(prefix == null) { prefix = ""; }
            string rval = "SELECT ";
            for(int i = 0; i < Internals.Length; i++)
            {
                if(i > 0) { rval += ", "; }
                rval += prefix.Trim() + Internals[i].ColumnName;
            }
            rval += (" FROM " + TableName);

            return rval;
        }


        /// <summary>Gets a field by its column name.</summary>
        /// <param name="columnName">Column name.</param>
        /// <returns>Field.</returns>
        public Field GetFieldForColumn(string columnName)
        {
            columnName = columnName.ToUpper();
            foreach(Field i in Internals)
            {
                if(i.ColumnName.ToUpper() == columnName) { return i; }
            }

            return null;
        }
    }
}
