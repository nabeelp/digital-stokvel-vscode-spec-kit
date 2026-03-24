import { useState } from 'react';
import { Formik, Form, Field, ErrorMessage } from 'formik';
import { apiService } from '../services/api';
import type { MakeContributionRequest } from '../services/api';
import './PayContribution.css';

interface PayContributionProps {
  groupId: string;
  groupName: string;
  contributionAmount: number;
  onClose: () => void;
  onSuccess: () => void;
}

export default function PayContribution({ groupId, groupName, contributionAmount, onClose, onSuccess }: PayContributionProps) {
  const [submitError, setSubmitError] = useState<string>('');
  const [submitSuccess, setSubmitSuccess] = useState<string>('');
  const [receipt, setReceipt] = useState<string>('');

  const initialValues = {
    amount: contributionAmount.toString(),
    paymentMethod: 'OneTap',
  };

  const validate = (values: typeof initialValues) => {
    const errors: Partial<Record<keyof typeof initialValues, string>> = {};

    const amount = parseFloat(values.amount);
    if (isNaN(amount) || amount <= 0) {
      errors.amount = 'Please enter a valid amount';
    } else if (amount !== contributionAmount) {
      errors.amount = `Amount must be R${contributionAmount} as per group contribution schedule`;
    }

    return errors;
  };

  const handleSubmit = async (
    values: typeof initialValues,
    { setSubmitting }: { setSubmitting: (isSubmitting: boolean) => void }
  ) => {
    setSubmitError('');
    setSubmitSuccess('');

    try {
      const request: MakeContributionRequest = {
        groupId,
        amount: parseFloat(values.amount),
        paymentMethod: values.paymentMethod,
      };

      // Generate idempotency key
      const idempotencyKey = `${groupId}-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;

      const response = await apiService.makeContribution(request, idempotencyKey);
      
      setSubmitSuccess(response.message || 'Your contribution was successful! 🎉');
      if (response.data.receipt) {
        setReceipt(response.data.receipt);
      }

      // Call success callback after 2 seconds
      setTimeout(() => {
        onSuccess();
        onClose();
      }, 2000);
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || error.message || 'Payment failed. Please try again.';
      setSubmitError(errorMessage);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Pay Contribution</h2>
          <button className="close-button" onClick={onClose}>
            ×
          </button>
        </div>

        <div className="modal-body">
          <div className="contribution-info">
            <div className="info-item">
              <span className="info-label">Group:</span>
              <span className="info-value">{groupName}</span>
            </div>
            <div className="info-item">
              <span className="info-label">Amount Due:</span>
              <span className="info-value">R{contributionAmount.toLocaleString('en-ZA', { minimumFractionDigits: 2 })}</span>
            </div>
          </div>

          {submitError && (
            <div className="alert alert-error">
              {submitError}
            </div>
          )}

          {submitSuccess && (
            <div className="alert alert-success">
              {submitSuccess}
              {receipt && (
                <div className="receipt-preview">
                  <h4>Receipt:</h4>
                  <pre>{receipt}</pre>
                </div>
              )}
            </div>
          )}

          {!submitSuccess && (
            <Formik
              initialValues={initialValues}
              validate={validate}
              onSubmit={handleSubmit}
            >
              {({ isSubmitting, errors, touched }) => (
                <Form className="payment-form">
                  <div className="form-group">
                    <label htmlFor="amount">Contribution Amount (ZAR)</label>
                    <Field
                      type="number"
                      id="amount"
                      name="amount"
                      readOnly
                      className={errors.amount && touched.amount ? 'error' : ''}
                    />
                    <ErrorMessage name="amount" component="div" className="error-message" />
                  </div>

                  <div className="form-group">
                    <label htmlFor="paymentMethod">Payment Method</label>
                    <Field as="select" id="paymentMethod" name="paymentMethod">
                      <option value="OneTap">One-Tap Payment</option>
                      <option value="DebitOrder">Debit Order</option>
                      <option value="USSD">USSD</option>
                    </Field>
                    <small className="form-hint">Choose your preferred payment method</small>
                  </div>

                  <div className="payment-security">
                    <div className="security-badge">
                      🔒 Secure Payment
                    </div>
                    <p>Your payment is protected by bank-grade encryption</p>
                  </div>

                  <div className="modal-actions">
                    <button
                      type="button"
                      className="btn btn-secondary"
                      onClick={onClose}
                      disabled={isSubmitting}
                    >
                      Cancel
                    </button>
                    <button
                      type="submit"
                      className="btn btn-primary"
                      disabled={isSubmitting}
                    >
                      {isSubmitting ? 'Processing...' : `Pay R${contributionAmount.toLocaleString()}`}
                    </button>
                  </div>
                </Form>
              )}
            </Formik>
          )}
        </div>
      </div>
    </div>
  );
}
