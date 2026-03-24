import { useState } from 'react';
import { Formik, Form, Field, ErrorMessage } from 'formik';
import { apiService } from '../services/api';
import './InviteMember.css';

interface InviteMemberProps {
  groupId: string;
  groupName: string;
  onClose: () => void;
  onSuccess: () => void;
}

export default function InviteMember({ groupId, groupName, onClose, onSuccess }: InviteMemberProps) {
  const [submitError, setSubmitError] = useState<string>('');
  const [submitSuccess, setSubmitSuccess] = useState<string>('');

  const validate = (values: { phoneNumber: string }) => {
    const errors: any = {};

    if (!values.phoneNumber) {
      errors.phoneNumber = 'Phone number is required';
    } else if (!/^27\d{9}$/.test(values.phoneNumber)) {
      errors.phoneNumber = 'Phone number must be in format 27XXXXXXXXX';
    }

    return errors;
  };

  const handleSubmit = async (values: { phoneNumber: string }, { setSubmitting, resetForm }: any) => {
    try {
      setSubmitError('');
      setSubmitSuccess('');

      await apiService.inviteMember(groupId, values.phoneNumber);

      setSubmitSuccess(`Invitation sent to ${values.phoneNumber}`);
      resetForm();
      
      setTimeout(() => {
        onSuccess();
        onClose();
      }, 2000);
    } catch (err: any) {
      setSubmitError(
        err.response?.data?.message || 
        'Failed to send invitation. Please try again.'
      );
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="invite-member-modal" onClick={onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Invite Member</h2>
          <button onClick={onClose} className="btn-close">
            ✕
          </button>
        </div>

        <div className="modal-body">
          <div className="group-info">
            <p><strong>Group:</strong> {groupName}</p>
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
            initialValues={{ phoneNumber: '' }}
            validate={validate}
            onSubmit={handleSubmit}
          >
            {({ isSubmitting }) => (
              <Form>
                <div className="form-group">
                  <label htmlFor="phoneNumber">Member Phone Number</label>
                  <Field
                    type="tel"
                    id="phoneNumber"
                    name="phoneNumber"
                    placeholder="27812345678"
                    className="form-input"
                    disabled={isSubmitting}
                  />
                  <ErrorMessage name="phoneNumber" component="div" className="error-message" />
                  <small className="form-help">
                    Enter the South African phone number starting with 27
                  </small>
                </div>

                <div className="info-box">
                  <p><strong>ℹ️ What happens next:</strong></p>
                  <ul>
                    <li>The member will receive an SMS invitation</li>
                    <li>They can accept or decline the invitation</li>
                    <li>Once accepted, they'll be added as a regular Member</li>
                    <li>You can assign roles after they join</li>
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
                    {isSubmitting ? 'Sending...' : 'Send Invitation'}
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
