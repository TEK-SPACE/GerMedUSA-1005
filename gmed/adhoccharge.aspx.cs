// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Data.SqlClient;
using System.Text;
using AspDotNetStorefrontCore;
using AspDotNetStorefrontGateways;

namespace AspDotNetStorefrontAdmin
{
	public partial class adhoccharge : AspDotNetStorefront.Admin.AdminPageBase
	{
		protected void Page_Load(object sender, System.EventArgs e)
		{
			StringBuilder writer = new StringBuilder();
			Response.CacheControl = "private";
			Response.Expires = 0;
			Response.AddHeader("pragma", "no-cache");

			int ONX = CommonLogic.QueryStringUSInt("OrderNumber");

			Customer ThisCustomer = Context.GetCustomer();
			int OrderCustomerID = 0;
			String OriginalTransactionID = String.Empty;
			String PM = String.Empty;

			using(var dbconn = new SqlConnection(DB.GetDBConn()))
			{
				dbconn.Open();
				using(var rs = DB.GetRS(String.Format("select CustomerID,AuthorizationPNREF,PaymentMethod from Orders with (NOLOCK) where OrderNumber={0}", ONX.ToString()), dbconn))
				{
					if(rs.Read())
					{
						OrderCustomerID = DB.RSFieldInt(rs, "CustomerID");
						OriginalTransactionID = DB.RSField(rs, "AuthorizationPNREF");
						PM = AppLogic.CleanPaymentMethod(DB.RSField(rs, "PaymentMethod"));
					}
				}
			}

			Customer OrderCustomer = new Customer(OrderCustomerID, true);

			String GW = AppLogic.ActivePaymentGatewayCleaned();

			if(PM == AppLogic.ro_PMPayPalExpress)
			{
				GW = Gateway.ro_GWPAYPAL;
			}

			bool GatewayRequiresCC = GatewayLoader.GetProcessor(GW).RequiresCCForFurtherProcessing();

			writer.Append("<div class=\"white-ui-box\">");
			if(!ThisCustomer.IsAdminUser)
			{
				writer.Append("<b><font color=red>" + AppLogic.GetString("admin.common.PermissionDeniedUC", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + "</b></font>");
			}
			else
			{
				if(ONX == 0 || OrderCustomerID == 0)
				{
					writer.Append("<div class=\"alert alert-danger\">" + AppLogic.GetString("adhoccharge.aspx.1", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + "</div>");
					writer.Append("<p><a href=\"javascript:self.close();\">" + AppLogic.GetString("admin.common.Close", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + "</a></p>");
				}
				else
				{

					Address BillingAddress = new Address();
					BillingAddress.LoadFromDB(OrderCustomer.PrimaryBillingAddressID);

					if (CommonLogic.FormBool("IsSubmit") && CommonLogic.FormCanBeDangerousContent("OrderTotal").Trim().Length != 0)
					{
						Decimal OrderTotal = CommonLogic.FormNativeDecimal("OrderTotal");
						String OrderDescription = CommonLogic.FormCanBeDangerousContent("Description");
						AppLogic.TransactionTypeEnum OrderType = (AppLogic.TransactionTypeEnum)Enum.Parse(typeof(AppLogic.TransactionTypeEnum), CommonLogic.FormCanBeDangerousContent("OrderType"), true);
						int NewOrderNumber = 0;
						if(OrderType == AppLogic.TransactionTypeEnum.CHARGE)
						{
							if(CommonLogic.FormCanBeDangerousContent("CardNumber").Length < 4)
							{
								Security.LogEvent(AppLogic.GetString("admin.common.ViewedCreditCard.Success", ThisCustomer.SkinID, ThisCustomer.LocaleSetting), String.Format(AppLogic.GetString("admin.adhoccharge.ViewedCardNumber", ThisCustomer.SkinID, ThisCustomer.LocaleSetting), CommonLogic.FormCanBeDangerousContent("CardNumber").Replace("*", "").Substring(CommonLogic.FormCanBeDangerousContent("CardNumber").Replace("*", "").Length).PadLeft(CommonLogic.FormCanBeDangerousContent("CardNumber").Replace("*", "").Length, '*'), ONX.ToString()), OrderCustomer.CustomerID, ThisCustomer.CustomerID, Convert.ToInt32(ThisCustomer.CurrentSessionID));
							}
							else
							{
								Security.LogEvent(AppLogic.GetString("admin.common.ViewedCreditCard.Success", ThisCustomer.SkinID, ThisCustomer.LocaleSetting), String.Format(AppLogic.GetString("admin.adhoccharge.ViewedCardNumber", ThisCustomer.SkinID, ThisCustomer.LocaleSetting), CommonLogic.FormCanBeDangerousContent("CardNumber").Replace("*", "").Substring(CommonLogic.FormCanBeDangerousContent("CardNumber").Replace("*", "").Length - 4).PadLeft(CommonLogic.FormCanBeDangerousContent("CardNumber").Replace("*", "").Length, '*'), ONX.ToString()), OrderCustomer.CustomerID, ThisCustomer.CustomerID, Convert.ToInt32(ThisCustomer.CurrentSessionID));
							}
						}
						// use the billing info in the form, as the store admin may have overridden what was in the db
						// NOTE: we are NOT going to save this new updated billing info however, it is really up to the customer
						// to change their billing info, or the store admin should edit their billing address in the customers account page area
						BillingAddress.CardName = CommonLogic.FormCanBeDangerousContent("CardName");
						// NOTE, this could be last4 at this point!! not a full CC #! that is ok, as this address will never be stored to the db anyway!
						BillingAddress.CardNumber = CommonLogic.FormCanBeDangerousContent("CardNumber").Replace("*", "");
						BillingAddress.CardType = CommonLogic.FormCanBeDangerousContent("CardType");
						BillingAddress.CardExpirationMonth = CommonLogic.FormCanBeDangerousContent("CardExpirationMonth");
						BillingAddress.CardExpirationYear = CommonLogic.FormCanBeDangerousContent("CardExpirationYear");
						BillingAddress.CardStartDate = CommonLogic.FormCanBeDangerousContent("CardStartDate").Trim().Replace(" ", "").Replace("/", "").Replace("\\", "");
						BillingAddress.CardIssueNumber = CommonLogic.FormCanBeDangerousContent("CardIssueNumber");
						String CardExtraCode = CommonLogic.FormCanBeDangerousContent("CardExtraCode");

						String Status = Gateway.MakeAdHocOrder(AppLogic.ActivePaymentGatewayCleaned(), ONX, OriginalTransactionID, OrderCustomer, BillingAddress, CardExtraCode, OrderTotal, OrderType, OrderDescription, out NewOrderNumber);

						//PABP Required cleanup of in-memory objects
						CardExtraCode = "11111";
						CardExtraCode = "00000";
						CardExtraCode = "11111";
						CardExtraCode = String.Empty;

						if(Status == AppLogic.ro_OK)
						{
							Response.Redirect(AppLogic.AdminLinkUrl("adhocchargecomplete.aspx") + "?ordernumber=" + NewOrderNumber.ToString());
						}
						else
						{
							Response.Write("<div class=\"alert alert-danger\">" + AppLogic.GetString("adhoccharge.aspx.3", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + "<br/>" + Status + "</div>");
						}
						Response.Write("<p><a href=\"javascript:self.close();\">" + AppLogic.GetString("admin.common.Close", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + "</a></p>");
					}
					else
					{
						writer.Append("<script type=\"text/javascript\">\n");
						writer.Append("var GatewayRequiresCC=" + CommonLogic.IIF(GatewayRequiresCC, "1", "0") + ";\n");
						writer.Append("function getSelectedRadio(buttonGroup) {\n");
						writer.Append("   // returns the array number of the selected radio button or -1 if no button is selected\n");
						writer.Append("   if (buttonGroup[0]) { // if the button group is an array (one button is not an array)\n");
						writer.Append("      for (var i=0; i<buttonGroup.length; i++) {\n");
						writer.Append("         if (buttonGroup[i].checked) {\n");
						writer.Append("            return i\n");
						writer.Append("         }\n");
						writer.Append("      }\n");
						writer.Append("   } else {\n");
						writer.Append("      if (buttonGroup.checked) { return 0; } // if the one button is checked, return zero\n");
						writer.Append("   }\n");
						writer.Append("   // if we get to this point, no radio button is selected\n");
						writer.Append("   return -1;\n");
						writer.Append("}");
						writer.Append("\n");
						writer.Append("function getSelectedRadioValue(buttonGroup) {\n");
						writer.Append("   // returns the value of the selected radio button or '' if no button is selected\n");
						writer.Append("   var i = getSelectedRadio(buttonGroup);\n");
						writer.Append("   if (i == -1) {\n");
						writer.Append("      return '';\n");
						writer.Append("   } else {\n");
						writer.Append("      if (buttonGroup[i]) { // Make sure the button group is an array (not just one button)\n");
						writer.Append("         return buttonGroup[i].value;\n");
						writer.Append("      } else { // The button group is just the one button, and it is checked\n");
						writer.Append("         return buttonGroup.value;\n");
						writer.Append("      }\n");
						writer.Append("   }\n");
						writer.Append("}");
						writer.Append("\n");
						writer.Append("function AdHocOrderTypeChanged()\n");
						writer.Append("{\n");
						writer.Append("	if(GatewayRequiresCC == 1 || getSelectedRadioValue(theForm.OrderType) == '" + AppLogic.TransactionTypeEnum.CHARGE.ToString() + "')\n");
						writer.Append("    {\n");
						writer.Append("        CCDiv.style.display = 'block';\n");
						writer.Append("    }\n");
						writer.Append("    else\n");
						writer.Append("    {\n");
						writer.Append("        CCDiv.style.display = 'none';\n");
						writer.Append("    }\n");
						writer.Append("}\n");
						writer.Append("function AdHocChargeOrRefundForm_Validator(theForm)\n");
						writer.Append("{\n");
						writer.Append("	submitonce(theForm);\n");
						writer.Append("	if(theForm.Description.value == '')\n");
						writer.Append("	{\n");
						writer.Append("		alert('" + AppLogic.GetString("adhoccharge.aspx.4", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + "');\n");
						writer.Append("		theForm.Description.focus();\n");
						writer.Append("		submitenabled(theForm);\n");
						writer.Append("		return (false);\n");
						writer.Append("	}\n");
						writer.Append("	if((getSelectedRadioValue(theForm.OrderType) == '" + AppLogic.TransactionTypeEnum.CHARGE.ToString() + "') || (GatewayRequiresCC == 1 && getSelectedRadioValue(theForm.OrderType) == '" + AppLogic.TransactionTypeEnum.CREDIT.ToString() + "'))\n");
						writer.Append("    {\n");
						writer.Append("        if(theForm.CardName.value == '')\n");
						writer.Append("	    {\n");
						writer.Append("		    alert('" + String.Format(AppLogic.GetString("adhoccharge.aspx.22", ThisCustomer.SkinID, ThisCustomer.LocaleSetting), "Name On Card") + "');\n");
						writer.Append("		    theForm.CardName.focus();\n");
						writer.Append("		    submitenabled(theForm);\n");
						writer.Append("		    return (false);\n");
						writer.Append("	    }\n");
						writer.Append("        if(theForm.CardNumber.value == '')\n");
						writer.Append("	    {\n");
						writer.Append("		    alert('" + String.Format(AppLogic.GetString("adhoccharge.aspx.22", ThisCustomer.SkinID, ThisCustomer.LocaleSetting), "Card Number") + "');\n");
						writer.Append("		    theForm.CardNumber.focus();\n");
						writer.Append("		    submitenabled(theForm);\n");
						writer.Append("		    return (false);\n");
						writer.Append("	    }\n");
						writer.Append("        if(isNaN(theForm.CardNumber.value))\n");
						writer.Append("	    {\n");
						writer.Append("		    alert('" + AppLogic.GetString("adhoccharge.aspx.28", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + "');\n");
						writer.Append("		    theForm.CardNumber.focus();\n");
						writer.Append("		    submitenabled(theForm);\n");
						writer.Append("		    return (false);\n");
						writer.Append("	    }\n");
						writer.Append("        if(theForm.CardExpirationMonth.value == '')\n");
						writer.Append("	    {\n");
						writer.Append("		    alert('" + String.Format(AppLogic.GetString("adhoccharge.aspx.22", ThisCustomer.SkinID, ThisCustomer.LocaleSetting), "Card Expiration Month") + "');\n");
						writer.Append("		    theForm.CardExpirationMonth.focus();\n");
						writer.Append("		    submitenabled(theForm);\n");
						writer.Append("		    return (false);\n");
						writer.Append("	    }\n");
						writer.Append("        if(theForm.CardExpirationYear.value == '')\n");
						writer.Append("	    {\n");
						writer.Append("		    alert('" + String.Format(AppLogic.GetString("adhoccharge.aspx.22", ThisCustomer.SkinID, ThisCustomer.LocaleSetting), "Card Expiration Year") + "');\n");
						writer.Append("		    theForm.CardExpirationYear.focus();\n");
						writer.Append("		    submitenabled(theForm);\n");
						writer.Append("		    return (false);\n");
						writer.Append("	    }\n");
						writer.Append("        if(theForm.CardType.selectedIndex < 1)\n");
						writer.Append("	    {\n");
						writer.Append("		    alert('" + String.Format(AppLogic.GetString("adhoccharge.aspx.22", ThisCustomer.SkinID, ThisCustomer.LocaleSetting), "Card Type") + "');\n");
						writer.Append("		    theForm.CardType.focus();\n");
						writer.Append("		    submitenabled(theForm);\n");
						writer.Append("		    return (false);\n");
						writer.Append("	    }\n");
						writer.Append("	}\n");
						writer.Append("	submitenabled(theForm);\n");
						writer.Append("	return (true);\n");
						writer.Append("}\n");
						writer.Append("</script>\n");
						writer.Append(String.Format(AppLogic.GetString("adhoccharge.aspx.5", ThisCustomer.SkinID, ThisCustomer.LocaleSetting), ONX.ToString()));
						writer.Append("<p>" + AppLogic.GetString("adhoccharge.aspx.6", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + "</p>");

						String CNM = BillingAddress.CardName;
						String CN = BillingAddress.CardNumber;
						String Last4 = String.Empty;
						String CExpMonth = BillingAddress.CardExpirationMonth;
						String CExpYear = BillingAddress.CardExpirationYear;
						String CardType = BillingAddress.CardType;
						if(CN.Length == 0)
						{
							// try to pull it from order record:
							using(var dbconn = new SqlConnection(DB.GetDBConn()))
							{
								dbconn.Open();
								using(var rs2 = DB.GetRS(String.Format("SELECT CardNumber, CardName, Last4, CardExpirationMonth, CardExpirationYear, CardNumber, CardType, {0} FROM Orders WHERE OrderNumber = {1}", AppLogic.AppConfig("OrdersCCSaltField"), ONX), dbconn))
								{
									if(rs2.Read())
									{
										CN = DB.RSField(rs2, "CardNumber");
										CNM = DB.RSField(rs2, "CardName");
										Last4 = DB.RSField(rs2, "Last4");
										CExpMonth = DB.RSField(rs2, "CardExpirationMonth");
										CExpYear = DB.RSField(rs2, "CardExpirationYear");
										CN = DB.RSField(rs2, "CardNumber");
										CN = Security.UnmungeString(CN, DB.RSField(rs2, AppLogic.AppConfig("OrdersCCSaltField")));
										if(CN.StartsWith(Security.ro_DecryptFailedPrefix, StringComparison.InvariantCultureIgnoreCase))
										{
											CN = DB.RSField(rs2, "CardNumber");
										}
										CardType = DB.RSField(rs2, "CardType");
									}
								}
							}
						}

						if(AppLogic.AppConfigBool("StoreCCInDB") && OrderCustomer.StoreCCInDB && CN.Length > 0)
						{
							Security.LogEvent(AppLogic.GetString("admin.common.ViewedCreditCard.Success", ThisCustomer.SkinID, ThisCustomer.LocaleSetting), String.Format(AppLogic.GetString("admin.adhoccharge.ViewedCardNumber", ThisCustomer.SkinID, ThisCustomer.LocaleSetting), CN.Replace("*", "").Substring(CN.Replace("*", "").Length - 4).PadLeft(CN.Replace("*", "").Length, '*'), ONX.ToString()), OrderCustomer.CustomerID, ThisCustomer.CustomerID, Convert.ToInt32(ThisCustomer.CurrentSessionID));
						}

						if(GatewayRequiresCC)
						{
							writer.Append("<div class=\"alert alert-info\">" + AppLogic.GetString("adhoccharge.aspx.11", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + "</div>");
						}
						else
						{
							writer.Append("<div class=\"alert alert-info\">" + AppLogic.GetString("adhoccharge.aspx.12", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + "</div>");
						}

						if(!OrderCustomer.StoreCCInDB)
						{
							writer.Append("<div class=\"alert alert-danger\">" + AppLogic.GetString("adhoccharge.aspx.13", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + "</div>");
						}

						if(CN.Length == 0 || CN == AppLogic.ro_CCNotStoredString)
						{
							writer.Append("<div class=\"alert alert-danger\">" + AppLogic.GetString("adhoccharge.aspx.14", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + "</div>");
						}

						if(OrderCustomer.PrimaryBillingAddressID == 0)
						{
							writer.Append("<div class=\"alert alert-danger\">" + AppLogic.GetString("adhoccharge.aspx.7", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + "</div>");
						}
						else if(CN.Length == 0 && Last4.Length == 0 && GW != Gateway.ro_GWPAYPAL)
						{
							writer.Append("<div class=\"alert alert-danger\">" + AppLogic.GetString("adhoccharge.aspx.8", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + "</div>");
						}
						else
						{
							if (CommonLogic.FormBool("IsSubmit") && CommonLogic.FormCanBeDangerousContent("OrderTotal").Trim().Length == 0)
							{
								writer.Append("<div class=\"alert alert-danger\">Please enter a valid charge amount</div>\n");
							}
							writer.Append("<form id=\"AdHocChargeOrRefundForm\" name=\"AdHocChargeOrRefundForm\" method=\"POST\" action=\"" + AppLogic.AdminLinkUrl("adhoccharge.aspx") + "?OrderNumber=" + ONX.ToString() + "\" >");
							writer.Append("	<div class=\"item-action-bar\">");
							writer.Append("	<input type=\"button\" value=\"" + AppLogic.GetString("admin.common.Close", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + "\" name=\"B2\" onClick=\"javascript:self.close()\" class=\"btn btn-default\">");
							writer.Append("	<input type=\"submit\" value=\"" + AppLogic.GetString("adhoccharge.aspx.21", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + "\" name=\"B1\" class=\"btn btn-primary\">");
							writer.Append("	</div>");
							writer.Append("<input type=\"hidden\" name=\"IsSubmit\" value=\"true\">\n");
							writer.Append("<table class=\"table\">");
							writer.Append("<tr><td>" + AppLogic.GetString("adhoccharge.aspx.9", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + " </td><td>" + OriginalTransactionID.ToString() + "</td></tr>");
							writer.Append("<tr><td>" + AppLogic.GetString("admin.label.CustomerID", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + " </td><td>" + OrderCustomer.CustomerID.ToString() + "</td></tr>");
							writer.Append("<tr><td>" + AppLogic.GetString("admin.label.CustomerName", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + " </td><td>" + OrderCustomer.FullName() + "</td></tr>");
							writer.Append("<tr><td>" + AppLogic.GetString("adhoccharge.aspx.27", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + " </td><td>" + BillingAddress.Phone + "</td></tr>");
							writer.Append("<tr><td>" + AppLogic.GetString("adhoccharge.aspx.16", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + " </td><td>");
							writer.Append("<input onClick=\"AdHocOrderTypeChanged()\" type=\"radio\" value=\"" + AppLogic.TransactionTypeEnum.CHARGE.ToString() + "\" id=\"ChargeOrderType\" name=\"OrderType\">" + AppLogic.GetString("adhoccharge.aspx.17", ThisCustomer.SkinID, ThisCustomer.LocaleSetting));
							writer.Append("&nbsp;&nbsp;&nbsp;&nbsp;");
							writer.Append("<input onClick=\"AdHocOrderTypeChanged()\" type=\"radio\" value=\"" + AppLogic.TransactionTypeEnum.CREDIT.ToString() + "\" id=\"RefundOrderType\" name=\"OrderType\" checked>" + AppLogic.GetString("adhoccharge.aspx.18", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + "</td></tr>");
							writer.Append("<tr><td>" + AppLogic.GetString("adhoccharge.aspx.19", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + " </td><td><input type=\"text\" name=\"OrderTotal\" size=\"7\"><input type=\"hidden\" name=\"OrderTotal_vldt\" value=\"[req][number][blankalert=" + AppLogic.GetString("adhoccharge.aspx.26", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + "][invalidalert=" + AppLogic.GetString("admin.common.ValidDollarAmountPrompt", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + "]\"> (xx.xx format)</td></tr>");
							writer.Append("<tr><td colspan=\"2\">");
							writer.Append("<div id=\"CCDiv\" name=\"CCDiv\" style=\"display:" + CommonLogic.IIF(GatewayRequiresCC, "block", "none") + ";\">");
							writer.Append("<table class=\"table\">");
							writer.Append("<tr>");
							writer.Append("<td align=\"right\">" + AppLogic.GetString("address.cs.31", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + "</td>");
							writer.Append("<td>\n");
							writer.Append("<select size=\"1\" name=\"CardType\" id=\"CardType\">");
							writer.Append("<option value=\"\">" + AppLogic.GetString("address.cs.32", ThisCustomer.SkinID, ThisCustomer.LocaleSetting));
							using(var dbconn = new SqlConnection(DB.GetDBConn()))
							{
								dbconn.Open();
								using(var rsCard = DB.GetRS("select * from creditcardtype  with (NOLOCK)  where Accepted=1 order by CardType", dbconn))
								{
									while(rsCard.Read())
									{
										writer.Append("<option value=\"" + DB.RSField(rsCard, "CardType") + "\" " + CommonLogic.IIF(CardType == DB.RSField(rsCard, "CardType"), " selected ", "") + ">" + DB.RSField(rsCard, "CardType") + "</option>\n");
									}

								}

							}
							writer.Append("</select>\n");
							writer.Append("</td>");
							writer.Append("</tr>");
							writer.Append("<tr><td align=\"right\">" + AppLogic.GetString("adhoccharge.aspx.10", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + " </td><td><input size=\"20\" maxlength=\"100\" type=\"text\" name=\"CardName\" id=\"CardName\" value=\"" + CNM + "\"></td></tr>");
							writer.Append("<tr><td align=\"right\">" + AppLogic.GetString("adhoccharge.aspx.24", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + " </td><td><input size=\"20\" maxlength=\"19\" type=\"text\" autocomplete=\"off\" name=\"CardNumber\" id=\"CardNumber\" value=\"" + CN + "\">&nbsp;" + String.Format(AppLogic.GetString("admin.adhoccharge.OriginalOrderLastFour", ThisCustomer.SkinID, ThisCustomer.LocaleSetting), Last4) + ")</td></tr>");
							writer.Append("<tr><td align=\"right\">" + AppLogic.GetString("adhoccharge.aspx.15", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + " </td><td><input size=\"4\" maxlength=\"4\" type=\"text\" autocomplete=\"off\" name=\"CardExtraCode\" id=\"CardExtraCode\">");
							writer.Append("<tr><td align=\"right\">" + AppLogic.GetString("adhoccharge.aspx.25", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + " </td><td><input type=\"text\" size=\"2\" maxlength=\"2\" name=\"CardExpirationMonth\" id=\"CardExpirationMonth\" value=\"" + CExpMonth + "\"> / <input size=\"4\" maxlength=\"4\" type=\"text\" name=\"CardExpirationYear\" id=\"CardExpirationYear\" value=\"" + CExpYear + "\"> (MM/YYYY)</td></tr>");
							if(AppLogic.AppConfigBool("ShowCardStartDateFields"))
							{
								writer.Append("<tr><td align=\"right\">" + AppLogic.GetString("address.cs.59", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + "</td><td><input type=\"text\" autocomplete=\"off\" name=\"CardStartDate\" id=\"CardStartDate\" size=\"5\" maxlength=\"20\"> " + AppLogic.GetString("address.cs.64", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + "</td></tr>");
								writer.Append("<tr><td align=\"right\">" + AppLogic.GetString("address.cs.61", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + "</td><td><input type=\"text\" autocomplete=\"off\" name=\"CardIssueNumber\" id=\"CardIssueNumber\" size=\"25\" maxlength=\"25\"> " + AppLogic.GetString("address.cs.63", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + "</td></tr>");
							}
							writer.Append("</table>");
							writer.Append("</div>");
							writer.Append("</td></tr>");
							writer.Append("</table>");
							writer.Append("	<p>" + AppLogic.GetString("adhoccharge.aspx.20", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + " </p>");
							writer.Append("	<p><textarea class=\"text-multiline\" id=\"Description\" name=\"Description\" style=\"width: 90%\"></textarea></p>");
							writer.Append("	<div class=\"item-action-bar\">");
							writer.Append("	<input type=\"button\" value=\"" + AppLogic.GetString("admin.common.Close", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + "\" name=\"B2\" onClick=\"javascript:self.close()\" class=\"btn btn-default\">");
							writer.Append("	<input type=\"submit\" value=\"" + AppLogic.GetString("adhoccharge.aspx.21", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + "\" name=\"B1\" onClick=\"return AdHocChargeOrRefundForm_Validator(this.form);\" class=\"btn btn-primary\">");
							writer.Append("	</div>");
							writer.Append("</form>");
						}
					}
				}
			}
			writer.Append("</div>");
			ltContent.Text = writer.ToString();
		}
	}
}
