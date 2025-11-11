using Microsoft.AspNetCore.Mvc;
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
    public class ContactService(IUnitOfWork unitOfWork, IAuditService _auditService, ILogger<ContactService> _logger, IWorkflowService _workflowService) : IContactService
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

            //check if contact already exists
            var existingContact = await unitOfWork.Contacts.FirstOrDefaultAsync(c => c.Email == contactDto.Email && c.OrganizationId == user.OrganizationId);
            if (existingContact != null)
            {
                if (existingContact.IsDeleted == true)
                {
                    // --- 3. AUDIT LOGIC FOR RE-ACTIVATION ---
                    var auditOwnerId = contactDto.OwnerId;

                    // Log activation
                    await _auditService.LogChangeAsync(new AuditLogDto
                    {
                        OwnerId = auditOwnerId ?? new Guid(),
                        RecordId = existingContact.Id,
                        TableName = "Contact",
                        FieldName = "IsDeleted",
                        OldValue = "true",
                        NewValue = "false"
                    });

                    // Log all changed fields
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

                    // Apply changes
                    existingContact.IsDeleted = false;
                    existingContact.FirstName = contactDto.FirstName;
                    existingContact.LastName = contactDto.LastName;
                    existingContact.Number = contactDto.Number;
                    existingContact.Email = contactDto.Email;
                    existingContact.LeadStatusId = contactDto.LeadStatusId;
                    existingContact.LifeCycleStageId = contactDto.LifeCycleStageId;
                    existingContact.OwnerId = contactDto.OwnerId;
                    unitOfWork.Contacts.Update(existingContact);

                    // Saves the Contact update AND all the Audit logs in one transaction
                    await unitOfWork.SaveChangesAsync();

                    try
                    {
                        await _workflowService.RunTriggersAsync(
                            WorkflowTrigger.ContactCreated,
                            existingContact // Pass the new contact object
                        );
                    }
                    catch (Exception ex)
                    {
                        // Log the error, but don't stop the user's action
                        _logger.LogError(ex, "Workflow failed for ContactCreated: {ContactId}", existingContact.Id);
                    }


                    return "Contact created successfully";


                }
                else
                {
                    return "Contact with this email already exists";
                }

            }

            // Map ContactDto to Contact entity
            var contact = new Contact
            {
                FirstName = contactDto.FirstName,
                LastName = contactDto.LastName,
                Email = contactDto.Email,
                Number = contactDto.Number,
                JobTitle = contactDto.JobTitle,
                LeadStatusId = contactDto.LeadStatusId ?? 1,
                LifeCycleStageId = contactDto.LifeCycleStageId,
                OrganizationId = user?.OrganizationId ?? 0,
                OwnerId = contactDto.OwnerId,
            };
            await unitOfWork.Contacts.AddAsync(contact);
            await unitOfWork.SaveChangesAsync(); // <-- First save to get the new contact.Id

            // --- 4. AUDIT LOGIC FOR NEW CONTACT ---
            try
            {
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

                // Save the audit logs
                await unitOfWork.SaveChangesAsync();
                try
                {
                    await _workflowService.RunTriggersAsync(
                        WorkflowTrigger.ContactCreated,
                        contact // Pass the new contact object
                    );
                }
                catch (Exception ex)
                {
                    // Log the error, but don't stop the user's action
                    _logger.LogError(ex, "Workflow failed for ContactCreated: {ContactId}", contact.Id);
                }
            }
            catch (Exception ex)
            {
                // Log the error, but the contact was already created.
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
            if (user.RoleId == 2 || user.RoleId == 3)
            {
                predicate = c => c.OrganizationId == user.OrganizationId && (!c.IsDeleted ?? false);
            }
            else
            {
                predicate = c => c.OwnerId == userId && c.OrganizationId == user.OrganizationId && (!c.IsDeleted ?? false);
            }

            // Eager load LeadStatus to avoid N+1 queries
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
                // Only log if the value is actually changing
                if (contact.IsDeleted == false)
                {
                    // Log the change
                    await _auditService.LogChangeAsync(new AuditLogDto
                    {
                        OwnerId = ownerId,
                        RecordId = contact.Id,
                        TableName = "Contact",
                        FieldName = "IsDeleted",
                        OldValue = "false",
                        NewValue = "true"
                    });

                    // Apply the change
                    contact.IsDeleted = true;
                    unitOfWork.Contacts.Update(contact);
                }
            }

            // Save all updates and all audit logs in one transaction
            await unitOfWork.SaveChangesAsync();
            return true;
        }




        public async Task<string> UpdateContact(ContactDto contact, Guid ownerId) // <-- ADDED ownerId
        {
            if (contact != null)
            {
                Contact existingContact = await unitOfWork.Contacts.FirstOrDefaultAsync(c => c.Id == contact.Id);
                if (existingContact != null)
                {
                    // --- NEW EMAIL VALIDATION LOGIC ---
                    if (existingContact.Email != contact.Email)
                    {
                        // Check if the NEW email is already taken by ANOTHER contact in the same org
                        var emailOwner = await unitOfWork.Contacts.FirstOrDefaultAsync(c =>
                            c.Email == contact.Email &&
                            c.OrganizationId == existingContact.OrganizationId &&
                            c.Id != existingContact.Id); // <-- Exclude the current contact

                        if (emailOwner != null)
                        {
                            // The email is taken by another record
                            if (emailOwner.IsDeleted == false)
                            {
                                return "Email is already in use by another active contact.";
                            }
                            else
                            {
                                // This is your exact scenario. The email belongs to a deleted contact.
                                return "Email belongs to a deleted contact. Please use a different email or re-activate the other contact.";
                            }
                        }
                    }
                    // --- END OF NEW VALIDATION LOGIC ---


                    // --- AUDIT LOGIC START ---

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
                    // --- AUDIT LOGIC END ---

                    unitOfWork.Contacts.Update(existingContact);
                    await unitOfWork.SaveChangesAsync(); // Saves all Contact changes and all Audit logs
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

            // Optional: Check if company exists
            var companyExists = await unitOfWork.Company.AnyAsync(c => c.Id == companyId);
            if (!companyExists)
                return (false, "Company not found.");

            // Check if the contact is already associated
            if (contact.CompanyId != null)
            {
                if (contact.CompanyId == companyId)
                {
                    // This is a success, just no work was needed.
                    return (true, "Contact is already associated with this company.");
                }
                else
                {
                    // This is the specific error you wanted to propagate.
                    return (false, "Contact already belongs to another company.");
                }
            }

            // --- AUDIT LOGIC ---
            await _auditService.LogChangeAsync(new AuditLogDto
            {
                OwnerId = ownerId,
                RecordId = contact.Id,
                TableName = "Contact",
                FieldName = "CompanyId",
                OldValue = "[NULL]",
                NewValue = companyId.ToString()
            });
            // --- END AUDIT LOGIC ---

            // If we are here, contact.CompanyId is null, so we can associate.
            contact.CompanyId = companyId;
            unitOfWork.Contacts.Update(contact);

            try
            {
                await unitOfWork.SaveChangesAsync(); // Saves contact AND audit log
                return (true, "Contact associated successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error associating contact to company: {ex.Message}");
                return (false, "A database error occurred.");
            }
        }

        // --- UPDATED METHOD ---
        public async Task<(bool Success, string Message)> AssociateContactToDealAsync(int contactId, int dealId, Guid ownerId)
        {
            // 1. Fetch the contact and include its existing deals
            var contact = await unitOfWork.Contacts.FirstOrDefaultAsync(
                c => c.Id == contactId,
                "Deals" // Eager load the Deals collection
            );

            if (contact == null)
            {
                Debug.WriteLine($"AssociateContactToDeal: Contact with ID {contactId} not found.");
                return (false, "Contact not found.");
            }

            // 2. Fetch the deal
            var deal = await unitOfWork.Deals.FirstOrDefaultAsync(d => d.Id == dealId);
            if (deal == null)
            {
                Debug.WriteLine($"AssociateContactToDeal: Deal with ID {dealId} not found.");
                return (false, "Deal not found.");
            }

            // 3. Check if the association already exists
            if (contact.Deals.Any(d => d.Id == dealId))
            {
                Debug.WriteLine($"AssociateContactToDeal: Contact {contactId} is already associated with Deal {dealId}.");
                return (true, "Contact is already associated with this deal.");
            }

            // --- AUDIT LOGIC ---
            await _auditService.LogChangeAsync(new AuditLogDto
            {
                OwnerId = ownerId,
                RecordId = contact.Id,
                TableName = "Contact",
                FieldName = "DealAssociation",
                OldValue = "[N/A]",
                NewValue = $"Associated with Deal ID: {deal.Id} ({deal.Name})"
            });
            // --- END AUDIT LOGIC ---

            // 4. Create the association
            contact.Deals.Add(deal);
            unitOfWork.Contacts.Update(contact);

            // 5. Save
            try
            {
                await unitOfWork.SaveChangesAsync(); // Saves contact AND audit log
                return (true, "Contact associated successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error associating contact to deal: {ex.Message}");
                return (false, "A database error occurred.");
            }
        }

        // --- UPDATED METHOD ---
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
