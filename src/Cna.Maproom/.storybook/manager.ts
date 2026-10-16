import { addons } from 'storybook/manager-api';
import { create } from 'storybook/theming';
addons.setConfig({
  theme: create({
    base: 'light',
    brandTitle: 'Field Atlas',
    brandUrl: '/?path=/docs/start-here--docs',
    brandTarget: '_self',
  }),
});
