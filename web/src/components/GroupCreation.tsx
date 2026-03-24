import { useState } from 'react';
import { Formik, Form, Field, ErrorMessage } from 'formik';
import { apiService } from '../services/api';
import type { CreateGroupRequest } from '../services/api';
import './GroupCreation.css';

interface GroupCreationValues {
  name: string;
  description: string;
  groupType: string;
  contributionAmount: string;
  contributionFrequency: string;
}

export default function GroupCreation() {
  const [submitError, setSubmitError] = useState<string>('');
  const [submitSuccess, setSubmitSuccess] = useState<string>('');

  const initialValues: GroupCreationValues = {
    name: '',
    description: '',
    groupType: 'Savings',
    contributionAmount: '',
    contributionFrequency: 'Monthly',
  };

  const validate = (values: GroupCreationValues) => {
    const errors: Partial<Record<keyof GroupCreationValues, string>> = {};

    // Name validation
    if (!values.name) {
      errors.name = 'Group name is required';
    } else if (values.name.length < 3) {
      errors.name = 'Group name must be at least 3 characters';
    } else if (values.name.length > 100) {
      errors.name = 'Group name must be less than 100 characters';
    }

    // Contribution amount validation
    if (!values.contributionAmount) {
      errors.contributionAmount = 'Contribution amount is required';
    } else {
      const amount = parseFloat(values.contributionAmount);
      if (isNaN(amount)) {
        errors.contributionAmount = 'Please enter a valid amount';
      } else if (amount < 50) {
        errors.contributionAmount = 'Minimum contribution is R50';
      } else if (amount > 100000) {
        errors.contributionAmount = 'Maximum contribution is R100,000';
      }
    }

    return errors;
  };

  const handleSubmit = async (
    values: GroupCreationValues,
    { setSubmitting, resetForm }: { setSubmitting: (isSubmitting: boolean) => void; resetForm: () => void }
  ) => {
    setSubmitError('');
    setSubmitSuccess('');

    try {
      const request: CreateGroupRequest = {
        name: values.name,
        description: values.description || undefined,
        groupType: values.groupType,
        contributionAmount: parseFloat(values.contributionAmount),
        contributionFrequency: values.contributionFrequency,
      };

      const response = await apiService.createGroup(request);
      
      setSubmitSuccess(`🎉 Group "${response.data.groupName}" created successfully! You are the Chairperson.`);
      resetForm();

      // Redirect to group dashboard after 2 seconds
      setTimeout(() => {
        window.location.href = `/groups/${response.data.groupId}`;
      }, 2000);
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || error.message || 'Failed to create group. Please try again.';
      setSubmitError(errorMessage);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="group-creation">
      <div className="group-creation-header">
        <h1>Create Your Stokvel Group</h1>
        <p>Start your community savings journey together</p>
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
        initialValues={initialValues}
        validate={validate}
        onSubmit={handleSubmit}
      >
        {({ isSubmitting, errors, touched }) => (
          <Form className="group-creation-form">
            <div className="form-group">
              <label htmlFor="name">Group Name *</label>
              <Field
                type="text"
                id="name"
                name="name"
                placeholder="e.g., Ubuntu Stokvel"
                className={errors.name && touched.name ? 'error' : ''}
              />
              <ErrorMessage name="name" component="div" className="error-message" />
            </div>

            <div className="form-group">
              <label htmlFor="description">Description</label>
              <Field
                as="textarea"
                id="description"
                name="description"
                placeholder="Tell your members about this group..."
                rows={3}
              />
            </div>

            <div className="form-group">
              <label htmlFor="groupType">Group Type *</label>
              <Field as="select" id="groupType" name="groupType">
                <option value="Savings">Savings</option>
                <option value="Burial">Burial</option>
                <option value="Investment">Investment</option>
                <option value="Grocery">Grocery</option>
              </Field>
            </div>

            <div className="form-group">
              <label htmlFor="contributionAmount">Monthly Contribution Amount (ZAR) *</label>
              <Field
                type="number"
                id="contributionAmount"
                name="contributionAmount"
                placeholder="Amount (R50 - R100,000)"
                min="50"
                max="100000"
                step="10"
                className={errors.contributionAmount && touched.contributionAmount ? 'error' : ''}
              />
              <ErrorMessage name="contributionAmount" component="div" className="error-message" />
              <small className="form-hint">Minimum R50, Maximum R100,000</small>
            </div>

            <div className="form-group">
              <label htmlFor="contributionFrequency">Contribution Frequency *</label>
              <Field as="select" id="contributionFrequency" name="contributionFrequency">
                <option value="Weekly">Weekly</option>
                <option value="Biweekly">Biweekly</option>
                <option value="Monthly">Monthly</option>
              </Field>
            </div>

            <button
              type="submit"
              className="btn btn-primary"
              disabled={isSubmitting}
            >
              {isSubmitting ? 'Creating...' : 'Create Group'}
            </button>
          </Form>
        )}
      </Formik>

      <div className="group-creation-info">
        <h3>What happens next?</h3>
        <ul>
          <li>✓ You become the Chairperson of your group</li>
          <li>✓ A group savings account is created</li>
          <li>✓ You can invite members via SMS</li>
          <li>✓ Members start contributing to build your pot</li>
          <li>✓ Your group earns tiered interest (3.5% - 5.5%)</li>
        </ul>
      </div>
    </div>
  );
}
