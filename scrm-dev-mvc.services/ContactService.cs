using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using scrm_dev_mvc.Data.Repository;
using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.DTO;
using scrm_dev_mvc.Models.Enums;
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.services.Interfaces;
using scrm_dev_mvc.Services;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;


namespace SCRM_dev.Services
{
    public class ContactService(IUnitOfWork unitOfWork, IAuditService _auditService, ILogger<ContactService> _logger, IWorkflowService _workflowService, IMemoryCache cache) : IContactService
    {
        
        public async Task<string> CreateContactAsync(ContactDto contactDto)
        {
            if (contactDto == null)
            {
                return null;
            }
            if (string.IsNullOrEmpty(contactDto.Email))
            {
                return null;
            }
            var user = await unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == contactDto.OwnerId);

            if (user == null)
                return null;

            var existingContact = await unitOfWork.Contacts.FirstOrDefaultAsync(c => c.Email == contactDto.Email && c.OrganizationId == user.OrganizationId);
            if (existingContact != null)
            {
                if (existingContact.IsDeleted == true)
                {
                    var auditOwnerId = contactDto.OwnerId;

                    await _auditService.LogChangeAsync(new AuditLogDto
                    {
                        OwnerId = auditOwnerId ?? new Guid(),
                        RecordId = existingContact.Id,
                        TableName = "Contact",
                        FieldName = "IsDeleted",
                        OldValue = "true",
                        NewValue = "false"
                    });

                    if (existingContact.FirstName != contactDto.FirstName)
                        await _auditService.LogChangeAsync(new AuditLogDto
                        {
                            OwnerId = auditOwnerId ?? new Guid(),
                            RecordId = existingContact.Id,
                            TableName = "Contact",
                            FieldName = "FirstName",
                            OldValue = existingContact.FirstName,
                            NewValue = contactDto.FirstName
                        });

                    if (existingContact.LastName != contactDto.LastName)
                        await _auditService.LogChangeAsync(new AuditLogDto
                        {
                            OwnerId = auditOwnerId ?? new Guid(),
                            RecordId = existingContact.Id,
                            TableName = "Contact",
                            FieldName = "LastName",
                            OldValue = existingContact.LastName,
                            NewValue = contactDto.LastName
                        });

                    if (existingContact.Number != contactDto.Number)
                        await _auditService.LogChangeAsync(new AuditLogDto
                        {
                            OwnerId = auditOwnerId ?? new Guid(),
                            RecordId = existingContact.Id,
                            TableName = "Contact",
                            FieldName = "Number",
                            OldValue = existingContact.Number,
                            NewValue = contactDto.Number
                        });

                    if (existingContact.LeadStatusId != contactDto.LeadStatusId)
                        await _auditService.LogChangeAsync(new AuditLogDto
                        {
                            OwnerId = auditOwnerId ?? new Guid(),
                            RecordId = existingContact.Id,
                            TableName = "Contact",
                            FieldName = "LeadStatusId",
                            OldValue = existingContact.LeadStatusId.ToString(),
                            NewValue = contactDto.LeadStatusId.ToString()
                        });

                    if (existingContact.LifeCycleStageId != contactDto.LifeCycleStageId)
                        await _auditService.LogChangeAsync(new AuditLogDto
                        {
                            OwnerId = auditOwnerId ?? new Guid(),
                            RecordId = existingContact.Id,
                            TableName = "Contact",
                            FieldName = "LifeCycleStageId",
                            OldValue = existingContact.LifeCycleStageId.ToString(),
                            NewValue = contactDto.LifeCycleStageId.ToString()
                        });

                    if (existingContact.OwnerId != contactDto.OwnerId)
                        await _auditService.LogChangeAsync(new AuditLogDto
                        {
                            OwnerId = auditOwnerId ?? new Guid(),
                            RecordId = existingContact.Id,
                            TableName = "Contact",
                            FieldName = "OwnerId",
                            OldValue = existingContact.OwnerId.ToString(),
                            NewValue = contactDto.OwnerId.ToString()
                        });

                    existingContact.IsDeleted = false;
                    existingContact.FirstName = contactDto.FirstName;
                    existingContact.LastName = contactDto.LastName;
                    existingContact.Number = contactDto.Number;
                    existingContact.Email = contactDto.Email;
                    existingContact.LeadStatusId = contactDto.LeadStatusId;
                    existingContact.LifeCycleStageId = contactDto.LifeCycleStageId;
                    existingContact.OwnerId = contactDto.OwnerId;
                    unitOfWork.Contacts.Update(existingContact);

                    await unitOfWork.SaveChangesAsync();

                    try
                    {
                        await _workflowService.RunTriggersAsync(
                            WorkflowTrigger.ContactCreated,
                            existingContact 
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Workflow failed for ContactCreated: {ContactId}", existingContact.Id);
                    }


                    return "Contact created successfully";


                }
                else
                {
                    return "Contact with this email already exists";
                }

            }

            var contact = new Contact
            {
                FirstName = contactDto.FirstName,
                LastName = contactDto.LastName,
                Email = contactDto.Email,
                Number = contactDto.Number,
                JobTitle = contactDto.JobTitle,
                LeadStatusId = contactDto.LeadStatusId ?? 1,
                LifeCycleStageId = contactDto.LifeCycleStageId ?? 1,
                OrganizationId = user?.OrganizationId ?? 0,
                OwnerId = contactDto.OwnerId,
            };
            await unitOfWork.Contacts.AddAsync(contact);
            await unitOfWork.SaveChangesAsync(); 

            try
            {
                #region Audit Code
                var auditOwnerId = contact.OwnerId;
                await _auditService.LogChangeAsync(new AuditLogDto
                {
                    OwnerId = auditOwnerId ?? new Guid(),
                    RecordId = contact.Id,
                    TableName = "Contact",
                    FieldName = "FirstName",
                    OldValue = "[NULL]",
                    NewValue = contact.FirstName
                });
                await _auditService.LogChangeAsync(new AuditLogDto
                {
                    OwnerId = auditOwnerId ?? new Guid(),
                    RecordId = contact.Id,
                    TableName = "Contact",
                    FieldName = "LastName",
                    OldValue = "[NULL]",
                    NewValue = contact.LastName
                });
                await _auditService.LogChangeAsync(new AuditLogDto
                {
                    OwnerId = auditOwnerId ?? new Guid(),
                    RecordId = contact.Id,
                    TableName = "Contact",
                    FieldName = "Email",
                    OldValue = "[NULL]",
                    NewValue = contact.Email
                });
                await _auditService.LogChangeAsync(new AuditLogDto
                {
                    OwnerId = auditOwnerId ?? new Guid(),
                    RecordId = contact.Id,
                    TableName = "Contact",
                    FieldName = "Number",
                    OldValue = "[NULL]",
                    NewValue = contact.Number
                });
                await _auditService.LogChangeAsync(new AuditLogDto
                {
                    OwnerId = auditOwnerId ?? new Guid(),
                    RecordId = contact.Id,
                    TableName = "Contact",
                    FieldName = "JobTitle",
                    OldValue = "[NULL]",
                    NewValue = contact.JobTitle
                });
                await _auditService.LogChangeAsync(new AuditLogDto
                {
                    OwnerId = auditOwnerId ?? new Guid(),
                    RecordId = contact.Id,
                    TableName = "Contact",
                    FieldName = "LeadStatusId",
                    OldValue = "[NULL]",
                    NewValue = contact.LeadStatusId.ToString()
                });
                await _auditService.LogChangeAsync(new AuditLogDto
                {
                    OwnerId = auditOwnerId ?? new Guid(),
                    RecordId = contact.Id,
                    TableName = "Contact",
                    FieldName = "LifeCycleStageId",
                    OldValue = "[NULL]",
                    NewValue = contact.LifeCycleStageId.ToString()
                });

                #endregion 
                await unitOfWork.SaveChangesAsync();
                try
                {
                    await _workflowService.RunTriggersAsync(
                        WorkflowTrigger.ContactCreated,
                        contact 
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Workflow failed for ContactCreated: {ContactId}", contact.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Contact {ContactId} was created, but failed to write audit logs.", contact.Id);
            }

            return "Contact created successfully";
        }


        public async Task<List<ContactResponseViewModel>> GetAllContacts(Guid userId)
        {

            var user = await unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                return new List<ContactResponseViewModel>();

            Expression<Func<Contact, bool>> predicate;
            if (user.RoleId == (int)UserRoleEnum.SalesAdminSuper || user.RoleId == (int)UserRoleEnum.SalesAdmin)
            {
                predicate = c => c.OrganizationId == user.OrganizationId && (!c.IsDeleted ?? false);
            }
            else
            {
                predicate = c => c.OwnerId == userId && c.OrganizationId == user.OrganizationId && (!c.IsDeleted ?? false);
            }

            var contacts = await unitOfWork.Contacts.GetAllAsync(predicate, asNoTracking: true, c => c.LeadStatus, c => c.Owner);

            var contactResponseViewModels = contacts.Select(contact => new ContactResponseViewModel
            {
                Id = contact.Id,
                Name = $"{contact.FirstName} {contact.LastName}",
                Email = contact.Email,
                PhoneNumber = contact.Number,
                LeadStatus = contact.LeadStatus?.LeadStatusName ?? "N/A",
                OwnerName = contact?.Owner?.Email ?? "N/A",
                CreatedAt = contact.CreatedAt,
            }).ToList();

            return contactResponseViewModels;
        }


        public async Task<List<ContactResponseViewModel>> GetAllContactsForCompany(Guid userId, int? companyId)
        {
            var user = await unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                return new List<ContactResponseViewModel>();

            Expression<Func<Contact, bool>> predicate;
            if(companyId == null)
            {
                if (user.RoleId == 2 || user.RoleId == 3)
                {
                    predicate = c => c.OrganizationId == user.OrganizationId && (!c.IsDeleted ?? false) ;
                }
                else
                {
                    predicate = c => c.OwnerId == userId && c.OrganizationId == user.OrganizationId && (!c.IsDeleted ?? false) ;
                }
            }
            else
            {
                if (user.RoleId == 2 || user.RoleId == 3)
                {
                    predicate = c => c.OrganizationId == user.OrganizationId && (!c.IsDeleted ?? false) && c.CompanyId == companyId;
                }
                else
                {
                    predicate = c => c.OwnerId == userId && c.OrganizationId == user.OrganizationId && (!c.IsDeleted ?? false) && c.CompanyId == companyId;
                }
            }
            

            var contacts = await unitOfWork.Contacts.GetAllAsync(predicate, asNoTracking: true, c => c.LeadStatus, c => c.Owner);

            var contactResponseViewModels = contacts.Select(contact => new ContactResponseViewModel
            {
                Id = contact.Id,
                Name = $"{contact.FirstName} {contact.LastName}",
                Email = contact.Email,
                PhoneNumber = contact.Number,
                LeadStatus = contact.LeadStatus?.LeadStatusName ?? "N/A",
                OwnerName = contact?.Owner?.Email ?? "N/A",
                CreatedAt = contact.CreatedAt,
            }).ToList();

            return contactResponseViewModels;
        }



        public async Task<IEnumerable<LeadStatus>> GetLeadStatusesAsync()
        {
            return await unitOfWork.LeadStatuses.GetAllAsync();
        }

        public async Task<IEnumerable<Lifecycle>> GetLifeCycleStagesAsync()
        {
            return await unitOfWork.Lifecycle.GetAllAsync();
        }

        public async Task<bool> DeleteContactsByIdsAsync(List<int> ids, Guid ownerId)
        {
            var contacts = await unitOfWork.Contacts.GetAllAsync(c => ids.Contains(c.Id));

            foreach (var contact in contacts)
            {
                if (contact.IsDeleted == false)
                {
                    await _auditService.LogChangeAsync(new AuditLogDto
                    {
                        OwnerId = ownerId,
                        RecordId = contact.Id,
                        TableName = "Contact",
                        FieldName = "IsDeleted",
                        OldValue = "false",
                        NewValue = "true"
                    });

                    contact.IsDeleted = true;
                    unitOfWork.Contacts.Update(contact);
                }
            }

            await unitOfWork.SaveChangesAsync();
            return true;
        }


        public async Task<string> UpdateContact(ContactDto contact, Guid ownerId) 
        {
            if (contact != null)
            {
                Contact existingContact = await unitOfWork.Contacts.FirstOrDefaultAsync(c => c.Id == contact.Id);
                if (existingContact != null)
                {
                    
                    if (existingContact.Email != contact.Email)
                    {
                        // Check if the NEW email is already taken by ANOTHER contact in the same org
                        var emailOwner = await unitOfWork.Contacts.FirstOrDefaultAsync(c =>
                            c.Email == contact.Email &&
                            c.OrganizationId == existingContact.OrganizationId &&
                            c.Id != existingContact.Id); //  Exclude the current contact

                        if (emailOwner != null)
                        {
                            // The email is taken by another record
                            if (emailOwner.IsDeleted == false)
                            {
                                return "Email is already in use by another active contact.";
                            }
                            else
                            {
                                return "Email belongs to a deleted contact. Please use a different email or re-activate the other contact.";
                            }
                        }
                    }


                    #region AUDIT

                    if (existingContact.FirstName != contact.FirstName)
                    {
                        await _auditService.LogChangeAsync(new AuditLogDto
                        {
                            OwnerId = ownerId,
                            RecordId = existingContact.Id,
                            TableName = "Contact",
                            FieldName = "FirstName",
                            OldValue = existingContact.FirstName,
                            NewValue = contact.FirstName
                        });
                        existingContact.FirstName = contact.FirstName;
                    }

                    if (existingContact.LastName != contact.LastName)
                    {
                        await _auditService.LogChangeAsync(new AuditLogDto
                        {
                            OwnerId = ownerId,
                            RecordId = existingContact.Id,
                            TableName = "Contact",
                            FieldName = "LastName",
                            OldValue = existingContact.LastName,
                            NewValue = contact.LastName
                        });
                        existingContact.LastName = contact.LastName;
                    }

                    if (existingContact.Email != contact.Email)
                    {
                        await _auditService.LogChangeAsync(new AuditLogDto
                        {
                            OwnerId = ownerId,
                            RecordId = existingContact.Id,
                            TableName = "Contact",
                            FieldName = "Email",
                            OldValue = existingContact.Email,
                            NewValue = contact.Email
                        });
                        existingContact.Email = contact.Email;
                    }

                    if (existingContact.Number != contact.Number)
                    {
                        await _auditService.LogChangeAsync(new AuditLogDto
                        {
                            OwnerId = ownerId,
                            RecordId = existingContact.Id,
                            TableName = "Contact",
                            FieldName = "Number",
                            OldValue = existingContact.Number,
                            NewValue = contact.Number
                        });
                        existingContact.Number = contact.Number;
                    }

                    if (existingContact.JobTitle != contact.JobTitle)
                    {
                        await _auditService.LogChangeAsync(new AuditLogDto
                        {
                            OwnerId = ownerId,
                            RecordId = existingContact.Id,
                            TableName = "Contact",
                            FieldName = "JobTitle",
                            OldValue = existingContact.JobTitle,
                            NewValue = contact.JobTitle
                        });
                        existingContact.JobTitle = contact.JobTitle;
                    }

                    if (existingContact.LeadStatusId != contact.LeadStatusId)
                    {
                        await _auditService.LogChangeAsync(new AuditLogDto
                        {
                            OwnerId = ownerId,
                            RecordId = existingContact.Id,
                            TableName = "Contact",
                            FieldName = "LeadStatusId",
                            OldValue = existingContact.LeadStatusId.ToString(),
                            NewValue = contact.LeadStatusId.ToString()
                        });
                        existingContact.LeadStatusId = contact.LeadStatusId;
                    }

                    if (existingContact.LifeCycleStageId != contact.LifeCycleStageId)
                    {
                        await _auditService.LogChangeAsync(new AuditLogDto
                        {
                            OwnerId = ownerId,
                            RecordId = existingContact.Id,
                            TableName = "Contact",
                            FieldName = "LifeCycleStageId",
                            OldValue = existingContact.LifeCycleStageId.ToString(),
                            NewValue = contact.LifeCycleStageId.ToString()
                        });
                        existingContact.LifeCycleStageId = contact.LifeCycleStageId;
                    }

                    if (existingContact.OwnerId != contact.OwnerId)
                    {
                        await _auditService.LogChangeAsync(new AuditLogDto
                        {
                            OwnerId = ownerId,
                            RecordId = existingContact.Id,
                            TableName = "Contact",
                            FieldName = "OwnerId",
                            OldValue = existingContact.OwnerId.ToString(),
                            NewValue = contact.OwnerId.ToString()
                        });
                        existingContact.OwnerId = contact.OwnerId;
                    }
                    #endregion 

                    unitOfWork.Contacts.Update(existingContact);
                    await unitOfWork.SaveChangesAsync();
                    return "Contact updated successfully";
                }
            }
            return "Contact updation Failed";
        }


        public Contact GetContactById(int id)
        {
            var contact = unitOfWork.Contacts.FirstOrDefaultAsync(c => c.Id == id, "LeadStatus,LifeCycleStage,Company,Deals,Activities.ActivityType").Result;
            return contact;
        }

        public async Task<(bool Success, string Message)> AssociateContactToCompany(int contactId, int companyId, Guid ownerId)
        {
            var contact = await unitOfWork.Contacts.FirstOrDefaultAsync(c => c.Id == contactId);
            if (contact == null)
                return (false, "Contact not found.");

            var companyExists = await unitOfWork.Company.AnyAsync(c => c.Id == companyId);
            if (!companyExists)
                return (false, "Company not found.");

            if (contact.CompanyId != null)
            {
                if (contact.CompanyId == companyId)
                {
                    return (true, "Contact is already associated with this company.");
                }
                else
                {
                    return (false, "Contact already belongs to another company.");
                }
            }

            await _auditService.LogChangeAsync(new AuditLogDto
            {
                OwnerId = ownerId,
                RecordId = contact.Id,
                TableName = "Contact",
                FieldName = "CompanyId",
                OldValue = "[NULL]",
                NewValue = companyId.ToString()
            });
            
            contact.CompanyId = companyId;
            unitOfWork.Contacts.Update(contact);

            try
            {
                await unitOfWork.SaveChangesAsync(); 
                return (true, "Contact associated successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error associating contact to company: {ex.Message}");
                return (false, "A database error occurred.");
            }
        }

        
        public async Task<(bool Success, string Message)> AssociateContactToDealAsync(int contactId, int dealId, Guid ownerId)
        {
            
            var contact = await unitOfWork.Contacts.FirstOrDefaultAsync(
                c => c.Id == contactId,
                "Deals" 
            );

            if (contact == null)
            {
                Debug.WriteLine($"AssociateContactToDeal: Contact with ID {contactId} not found.");
                return (false, "Contact not found.");
            }

            
            var deal = await unitOfWork.Deals.FirstOrDefaultAsync(d => d.Id == dealId);
            if (deal == null)
            {
                Debug.WriteLine($"AssociateContactToDeal: Deal with ID {dealId} not found.");
                return (false, "Deal not found.");
            }

            
            if (contact.Deals.Any(d => d.Id == dealId))
            {
                Debug.WriteLine($"AssociateContactToDeal: Contact {contactId} is already associated with Deal {dealId}.");
                return (true, "Contact is already associated with this deal.");
            }

            
            await _auditService.LogChangeAsync(new AuditLogDto
            {
                OwnerId = ownerId,
                RecordId = contact.Id,
                TableName = "Contact",
                FieldName = "DealAssociation",
                OldValue = "[N/A]",
                NewValue = $"Associated with Deal ID: {deal.Id} ({deal.Name})"
            });
            
            contact.Deals.Add(deal);
            unitOfWork.Contacts.Update(contact);

            
            try
            {
                await unitOfWork.SaveChangesAsync(); 
                return (true, "Contact associated successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error associating contact to deal: {ex.Message}");
                return (false, "A database error occurred.");
            }
        }

        
        public async Task<(bool Success, string Message)> DisassociateContactFromDealAsync(int contactId, int dealId, Guid ownerId)
        {
            
            var contact = await unitOfWork.Contacts.FirstOrDefaultAsync(
                c => c.Id == contactId,
                "Deals" // Eager load the Deals collection
            );

            if (contact == null)
            {
                Debug.WriteLine($"DisassociateContactFromDeal: Contact with ID {contactId} not found.");
                return (false, "Contact not found.");
            }

            // 2. Find the deal to remove *from the contact's loaded collection*
            var dealToRemove = contact.Deals.FirstOrDefault(d => d.Id == dealId);

            // 3. Check if the association even exists.
            if (dealToRemove == null)
            {
                Debug.WriteLine($"DisassociateContactFromDeal: Contact {contactId} is not associated with Deal {dealId}.");
                // Not an error, the desired state (disassociated) is already met.
                return (true, "Contact was not associated with this deal.");
            }

            // --- AUDIT LOGIC ---
            await _auditService.LogChangeAsync(new AuditLogDto
            {
                OwnerId = ownerId,
                RecordId = contact.Id,
                TableName = "Contact",
                FieldName = "DealAssociation",
                OldValue = $"Associated with Deal ID: {dealToRemove.Id} ({dealToRemove.Name})",
                NewValue = "[Disassociated]"
            });
            // --- END AUDIT LOGIC ---

            // 4. Remove the association
            contact.Deals.Remove(dealToRemove);
            unitOfWork.Contacts.Update(contact);

            // 5. Save
            try
            {
                await unitOfWork.SaveChangesAsync(); // Saves contact change AND audit log
                return (true, "Contact disassociated successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disassociating contact from deal: {ex.Message}");
                return (false, "A database error occurred.");
            }
        }

        public async Task<(bool Success, string Message)> DisassociateCompany(int contactId, Guid ownerId)
        {
            var contact = await unitOfWork.Contacts.FirstOrDefaultAsync(c => c.Id == contactId);

            // 1. Check if contact exists
            if (contact == null)
                return (false, "Contact not found.");

            // 2. Check if it's already disassociated
            if (contact.CompanyId == null)
                return (true, "Contact is already not associated with any company.");

            // Store old value for logging
            var oldCompanyId = contact.CompanyId.ToString();

            // --- AUDIT LOGIC ---
            await _auditService.LogChangeAsync(new AuditLogDto
            {
                OwnerId = ownerId,
                RecordId = contact.Id,
                TableName = "Contact",
                FieldName = "CompanyId",
                OldValue = oldCompanyId,
                NewValue = "[NULL]"
            });
            // --- END AUDIT LOGIC ---

            // 3. Perform the disassociation
            contact.CompanyId = null;
            unitOfWork.Contacts.Update(contact);

            try
            {
                await unitOfWork.SaveChangesAsync(); // Saves contact AND audit log
                return (true, "Company disassociated successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disassociating company: {ex.Message}");
                return (false, "A database error occurred.");
            }
        }

        public Task<List<Contact>> GetAllContactsByOwnerIdAsync(Guid ownerId)
        {
            return unitOfWork.Contacts.GetAllAsync(c => c.OwnerId == ownerId);
        }
    }
}
