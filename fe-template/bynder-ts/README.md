## Available Scripts

In the project directory, you can run:

### `npm start`

Runs the app in the development mode.\
Open [http://localhost:3000](http://localhost:3000) to view it in the browser.

The page will reload if you make edits.\
You will also see any lint errors in the console.

### `npm run build`

Builds the app for production to the `build` folder.\
It correctly bundles React in production mode and optimizes the build for the best performance.

When building, "react-app-rewired" is used and the hash version is removed.
The _config-overrides.js_ file was created specifically for this.
A path is set that corresponds to the path in sitecore.
Therefore, the path to the static folder in the final build is longer than usual.

`/sitecore modules/shell/ContentWorkflow/static`
instead of
`/static`

# project features

The file structure is typical, as in most applications.

Icons are converted to base64

For local development, to see different modals, you need to change the commented lines in _\fe-template\bynder-ts\public\index.html_

The file _\fe-template\bynder-ts\public\exampleToUse.html_ has the structure that the final markup should have to be inserted into the modal window. For example _\sitecore modules\Shell\ContentWorkflow\Import\MultiLocationImport.html_. Not related to local development.

Icons in the _\fe-template\bynder-ts\public\icon_ folders are used to display icons locally in the Tree. Are not mandatory.

### `npm run eject`

**Note: this is a one-way operation. Once you `eject`, you can't go back!**

If you aren't satisfied with the build tool and configuration choices, you can `eject` at any time. This command will remove the single build dependency from your project.

Instead, it will copy all the configuration files and the transitive dependencies (webpack, Babel, ESLint, etc) right into your project so you have full control over them. All of the commands except `eject` will still work, but they will point to the copied scripts so you can tweak them. At this point you're on your own.

You don't have to ever use `eject`. The curated feature set is suitable for small and middle deployments, and you shouldn't feel obligated to use this feature. However we understand that this tool wouldn't be useful if you couldn't customize it when you are ready for it.
