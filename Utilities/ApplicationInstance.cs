using System;

namespace ZBrad.FabLibs.Utilities
{
    /// <summary>
    /// application instance information
    /// </summary>
    public class ApplicationInstance
    {
        /// <summary>
        /// creates an empty instance
        /// </summary>
        public ApplicationInstance()
        {
        }

        /// <summary>
        /// creates an instance with supplied values
        /// </summary>
        /// <param name="name">application instance address</param>
        /// <param name="type">application type name</param>
        /// <param name="version">application version</param>
        /// <param name="status">application status</param>
        public ApplicationInstance(Uri name, string type, string version, string status)
        {
            this.Name = name;
            this.Type = type;
            this.Version = version;
            this.Status = status;
        }

        /// <summary>
        /// the application instance address
        /// </summary>
        public Uri Name { get; set; }

        /// <summary>
        /// the application type name
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// the application version
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// the application status
        /// </summary>
        public string Status { get; set; }
    }
}
