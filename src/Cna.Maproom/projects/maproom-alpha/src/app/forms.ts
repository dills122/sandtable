import { provideFormlyCore } from '@ngx-formly/core';
import { withFormlyMaterial } from '@ngx-formly/material';
export function provideMaproomForms() {
  return provideFormlyCore([
    ...withFormlyMaterial(),
    { validationMessages: [{ name: 'required', message: 'Choose a destination.' }] },
  ]);
}
