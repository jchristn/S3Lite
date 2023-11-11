﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace S3Lite.ApiObjects
{
    /// <summary>
    /// Amazon S3 bucket.
    /// </summary>
    [XmlRoot(ElementName = "Bucket", IsNullable = true)]
    public class Bucket
    {
        // Namespace = "http://s3.amazonaws.com/doc/2006-03-01/"

        #region Public-Members

        /// <summary>
        /// The name of the bucket.
        /// </summary>
        [XmlElement(ElementName = "Name", IsNullable = true)]
        public string Name { get; set; } = null;

        /// <summary>
        /// The timestamp from bucket creation.
        /// </summary>
        [XmlElement(ElementName = "CreationDate")]
        public DateTime CreationDate { get; set; } = DateTime.Now.ToUniversalTime();

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public Bucket()
        {

        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="name">Bucket name.</param>
        /// <param name="creation">Creation timestamp.</param>
        public Bucket(string name, DateTime creation)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            Name = name;
            CreationDate = creation;
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
