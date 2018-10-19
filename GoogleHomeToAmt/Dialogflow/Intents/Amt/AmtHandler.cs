// Copyright(c) 2018 Google Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not
// use this file except in compliance with the License. You may obtain a copy of
// the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
// License for the specific language governing permissions and limitations under
// the License.
using EpfServiceReference;
using Google.Cloud.Dialogflow.V2;
using Google.Cloud.Translation.V2;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using static EpfServiceReference.AssetStatisticIntegrationServiceClient;

namespace GoogleHomeToAmt.Dialogflow.Intents
{
    /// <summary>
    /// Handler for "translation.english" DialogFlow intent.
    /// </summary>
    [Intent("amt.recordusage")]
    public class AmtHandler : BaseHandler
    {
        /// <summary>
        /// Initializes class with the conversation.
        /// </summary>
        /// <param name="conversation"></param>
        public AmtHandler(Conversation conversation) : base(conversation)
        {
        }

        public string ModelCode { get; set; }
        public string SerialNumber { get; set; }
        public string UOM { get; set; }
        public decimal UsageReading { get; set; }
        public DateTime ReadingDate { get; set; }

        /// <summary>
        /// Handle the intent.
        /// </summary>
        /// <param name="req">Webhook request</param>
        /// <returns>Webhook response</returns>
        public override async Task<WebhookResponse> HandleAsync(WebhookRequest req)
        {
            try
            {
                ParseParameters(req);

                var processUsageReadingType = new ProcessUsageReadingType
                {
                    ApplicationArea = new ApplicationAreaRequestType
                    {
                        Sender = new SenderType
                        {
                            SendingSystemID = new IdentifierType { Value = "AMT" },
                            SendingSystemName = new TextType { Value = "Finning" }
                        },
                        CreationDateTime = new DateTimeType { Value = DateTime.MinValue },
                        MessageID = new IdentifierType { },
                        CorrelationID = new IdentifierType { },
                        ReplyTo = new ReplyToType
                        {
                            QueueName = new TextType { },
                            TopicName = new TextType { }
                        },
                        Result = new MessageResultType
                        {
                            ResultStatus = ResultStatusType.Success,
                            ErrorCode = new IdentifierType { },
                            ErrorMessage = new ErrorMessageType { }
                        }
                    },
                    DataArea = new ProcessUsageReadingTypeDataArea
                    {
                        Process = new UsageReadingType[]
                        {
                            new UsageReadingType
                            {
                                ID = Guid.NewGuid().ToString(),
                                AssetID = new AssetIDType
                                {
                                    ModelCode = new ModelCodeType { Value = ModelCode },
                                    SerialNumber = new SerialNumberType { Value = SerialNumber },
                                    RegistrationCounter = new AssetRegistrationCounterType { Value = "" }
                                },
                                MeterReading = new MeterReadingType[]
                                {
                                    new MeterReadingType
                                    {
                                        ID = Guid.NewGuid().ToString(),
                                        Items = new DateTimeType[]
                                        {
                                            new DateTimeType
                                            {
                                                Value = ReadingDate
                                            }
                                        },
                                        ItemsElementName = new ItemsChoiceType[] {
                                            ItemsChoiceType.MeterReadingDate
                                        },
                                        MeterUOMCode = new UnitOfMeasureType { Value = UOM },
                                        MeterReadingValue = new NumericType { Value = UsageReading },
                                        MeterReadingTypeCode = new MeterReadingTypeCodeType { Value = "C" }
                                    }
                                }
                            }
                        }
                    }
                };

                var client = new AssetStatisticIntegrationServiceClient(EndpointConfiguration.AssetStatisticIntegrationService_Http_Basic);
                client.ClientCredentials.UserName.UserName = "isipl\\gary.lu";
                client.ClientCredentials.UserName.Password = "Linkme2016july";
                var r = await client.ProcessUsageReadingAsync(processUsageReadingType);

                //DialogflowApp.Show($"<div>'{englishPhrase}' is '{response.TranslatedText}' in {languageCode}</div>");
                //return new WebhookResponse { FulfillmentText = response.TranslatedText };
                switch (r.ApplicationArea.Result.ResultStatus)
                {
                    case ResultStatusType.Warning:
                        DialogflowApp.Show($"<div>{r.DataArea[0].ErrorMessage.Value}</div>");
                        return new WebhookResponse { FulfillmentText = $"Warning: {r.DataArea[0].ErrorMessage.Value}" };
                    case ResultStatusType.Failure:
                        DialogflowApp.Show($"<div>{r.DataArea[0].ErrorMessage.Value}</div>");
                        return new WebhookResponse { FulfillmentText = $"Failure: {r.DataArea[0].ErrorMessage.Value}" };
                }
                DialogflowApp.Show($"<div>Usage Reading Recorded.</div>");
                return new WebhookResponse { FulfillmentText = "Usage Reading Recorded." };
            }
            catch (Exception e)
            {
                DialogflowApp.Show($"<div>{e.Message} {e.StackTrace}</div>");
                return new WebhookResponse { FulfillmentText = e.Message };
            }
        }

        private void ParseParameters(WebhookRequest req)
        {
            ModelCode = req.QueryResult.Parameters.Fields["modelCode"].StringValue;
            if (string.IsNullOrEmpty(ModelCode))
                throw new Exception("What is the Model Code of your equipment?");

            SerialNumber = req.QueryResult.Parameters.Fields["serialNumber"].StringValue;
            if (string.IsNullOrEmpty(SerialNumber))
                throw new Exception("What is the Serial Number of your equipment? ");

            UOM = req.QueryResult.Parameters.Fields["uom"].StringValue;
            if (string.IsNullOrEmpty(UOM))
                throw new Exception("What is your Unit of Measure?");

            UsageReading = (decimal)req.QueryResult.Parameters.Fields["usageReading"].NumberValue;
            if (!(UsageReading > 0))
                throw new Exception("What is your reading? It should be greater than 0.");

            try
            {
                ReadingDate = DateTime.Parse(req.QueryResult.Parameters.Fields["readingDate"].StringValue);
            }
            catch (Exception e)
            {
                throw new Exception("What is your reading date? I don't get it. " + e.Message);
            }
            if (ReadingDate == DateTime.MinValue)
                throw new Exception("What is your reading date? I don't get it.");
        }
    }
}