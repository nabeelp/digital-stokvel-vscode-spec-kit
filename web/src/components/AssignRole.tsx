import { useState } from 'react';
import { Formik, Form, Field, ErrorMessage } from 'formik';
import { apiService } from '../services/api';
import './AssignRole.css';

interface AssignRoleProps {
  groupId: string;
  groupName: string;
  memberPhone: string;
  currentRole: string;
  onClose: () => void;
  onSuccess: () => void;
}

export default function AssignRole({ groupId, groupName, memberPhone, currentRole, onClose, onSuccess }: AssignRoleProps) {
  const [submitError, setSubmitError] = useState<string>('');
  const [submitSuccess, setSubmitSuccess] = useState<string>('');

  const roleOptions = [
    { value: 'Chairperson', label: 'Chairperson', description: 'Group leader with full administrative rights' },
    { value: 'Treasurer', label: 'Treasurer', description: 'Manages finances and contributions' },
    { value: 'Secretary', label: 'Secretary', description: 'Maintains records and communications' },
    { value: 'Member', label: 'Member', description: 'Regular member with standard rights' },
  ];

  const validate = (values: { role: string }) => {
    const errors: any = {};

    if (!values.role) {
      errors.role = 'Please select a role';
    }

    if (values.role === currentRole) {
      errors.role = 'Member already has this role';
    }

    return errors;
  };

  const handleSubmit = async (values: { role: string }, { setSubmitting }: any) => {
    try {
      setSubmitError('');
      setSubmitSuccess('');

      await apiService.assignRole(groupId, memberPhone, values.role);

      setSubmitSuccess(`Role changed to ${values.role} successfully`);
      
      setTimeout(() => {
        onSuccess();
        onClose();
      }, 2000);
    } catch (err: any) {
      setSubmitError(
        err.response?.data?.message || 
        'Failed to assign role. Please try again.'
      );
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="assign-role-modal" onClick={onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Assign Role</h2>
          <button onClick={onClose} className="btn-close">
            ✕
          </button>
        </div>

        <div className="modal-body">
          <div className="member-info">
            <p><strong>Group:</strong> {groupName}</p>
            <p><strong>Member:</strong> {memberPhone}</p>
            <p><strong>Current Role:</strong> <span className="current-role">{currentRole}</span></p>
          </div>

          {submitError && (
            <div className="alert alert-error">
              {submitError}
            </div>
          )}

          {submitSuccess && (
            <div className="alert alert-success">
              {submitSuccess}
            </div>
          )}

          <Formik
            initialValues={{ role: currentRole }}
            validate={validate}
            onSubmit={handleSubmit}
          >
            {({ isSubmitting, values }) => (
              <Form>
                <div className="form-group">
                  <label htmlFor="role">Select New Role</label>
                  <div className="role-options">
                    {roleOptions.map((option) => (
                      <label
                        key={option.value}
                        className={`role-option ${values.role === option.value ? 'selected' : ''} ${option.value === currentRole ? 'current' : ''}`}
                      >
                        <Field
                          type="radio"
                          name="role"
                          value={option.value}
                          disabled={isSubmitting}
                        />
                        <div className="role-option-content">
                          <div className="role-option-label">
                            {option.label}
                            {option.value === currentRole && (
                              <span className="badge-current">Current</span>
                            )}
                          </div>
                          <div className="role-option-description">
                            {option.description}
                          </div>
                        </div>
                      </label>
                    ))}
                  </div>
                  <ErrorMessage name="role" component="div" className="error-message" />
                </div>

                <div className="warning-box">
                  <p><strong>⚠️ Important:</strong></p>
                  <ul>
                    <li><strong>Chairperson</strong>: Only one per group. Current Chairperson will become a Member.</li>
                    <li><strong>Treasurer/Secretary</strong>: Can have multiple, but recommend 1-2 each.</li>
                    <li><strong>Member</strong>: Standard access without administrative privileges.</li>
                    <li>Role changes take effect immediately.</li>
                  </ul>
                </div>

                <div className="modal-actions">
                  <button
                    type="button"
                    onClick={onClose}
                    className="btn btn-secondary"
                    disabled={isSubmitting}
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    className="btn btn-primary"
                    disabled={isSubmitting}
                  >
                    {isSubmitting ? 'Assigning...' : 'Assign Role'}
                  </button>
                </div>
              </Form>
            )}
          </Formik>
        </div>
      </div>
    </div>
  );
}
