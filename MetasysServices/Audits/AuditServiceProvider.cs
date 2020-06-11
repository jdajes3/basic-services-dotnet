﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Resources;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JohnsonControls.Metasys.BasicServices
{
    /// <summary>
    /// Provide audit item for the endpoints of the Metasys Audit API.
    /// </summary>
    public sealed class AuditServiceProvider : BasicServiceProvider, IAuditService
    {

        private const string BaseParam = "audits";
        private readonly IFlurlClient client;


        /// <summary>
        /// Initializes a new instance of <see cref="AuditServiceProvider"/> with supplied data.
        /// </summary>
        /// <param name="client">The FlurlClient to get response from URL.</param>
        /// <param name="version">The server's Api version.</param>
        /// <param name="logClientErrors">Set this flag to false to disable logging of client errors.</param>
        public AuditServiceProvider(IFlurlClient client, ApiVersion version, bool logClientErrors = true) :base(client, version, logClientErrors)
        {           
        }

        /// <inheritdoc/>
        public async Task<Audit> FindByIdAsync(Guid auditId)
        {
            var response = await GetRequestAsync("audits", null, auditId).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<Audit>(response.ToString());
        }

        /// <inheritdoc/>
        public async Task<PagedResult<Audit>> GetAsync(AuditFilter auditFilter)
        {
            return await GetPagedResultsAsync<Audit>("audits", ToDictionary(auditFilter)).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<PagedResult<Audit>> GetForObjectAsync(Guid objectId, AuditFilter auditFilter)
        {
            return await GetPagedResultsAsync<Audit>("objects", ToDictionary(auditFilter), objectId, BaseParam).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Audit FindById(Guid auditId)
        {
            return FindByIdAsync(auditId).GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public PagedResult<Audit> Get(AuditFilter auditFilter)
        {
            return GetAsync(auditFilter).GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public PagedResult<Audit> GetForObject(Guid objectId, AuditFilter auditFilter)
        {
            return GetForObjectAsync(objectId, auditFilter).GetAwaiter().GetResult();
        }
        /// <inheritdoc/>
        public IEnumerable<AuditAnnotation> GetAnnotations(Guid auditId)
        {
            return GetAuditAsync(auditId).GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<AuditAnnotation>> GetAuditAsync(Guid auditId)
        {
            // Retrieve JSON collection of Annotation
            var annotations = await GetAllAvailablePagesAsync("audits", null, auditId.ToString(), "annotations");
            List<AuditAnnotation> annotationsList = new List<AuditAnnotation>();
            // Convert to a collection of AuditAnnotation          
            foreach (var token in annotations)
            {
                AuditAnnotation auditAnnotation = new AuditAnnotation();
                // Build AlarmAnnotation object
                try
                {
                    auditAnnotation.Text = token["text"].Value<string>();
                    auditAnnotation.User = token["user"].Value<string>();
                    auditAnnotation.CreationTime = token["creationTime"].Value<DateTime>();
                    auditAnnotation.Action = token["action"].Value<string>();
                    auditAnnotation.AuditUrl = token["auditUrl"].Value<string>();
                    annotationsList.Add(auditAnnotation);
                }
                catch (Exception e)
                {
                    throw new MetasysObjectException(token.ToString(), e);
                }
            }
            return annotationsList;
        }

        /// <inheritdoc/>
        public void DiscardAudit(Guid id)
        {
            DiscardAuditAsync(id).GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public async Task DiscardAuditAsync(Guid id)
        {
            try
            {
                if (Version >= ApiVersion.v3)
                {
                    var response = await Client.Request(new Url("audits")
                    .AppendPathSegments(id, "discard"))
                    .PutJsonAsync(null)
                    .ConfigureAwait(false);
                }
                else
                {
                    throw new MetasysUnsupportedApiVersion(Version.ToString());
                }
            }
            catch (FlurlHttpException e)
            {
                ThrowHttpException(e);
            }
        }



    }
}