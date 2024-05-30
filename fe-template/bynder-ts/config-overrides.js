// react-app-rewired
module.exports = function override(config, env) {
  if (env !== "production") {
    return config;
  }

  const jsSitecorePath = "./sitecore modules/shell/ContentWorkflow/static/js/";
  const cssSitecorePath =
    "./sitecore modules/shell/ContentWorkflow/static/css/";

  // Get rid of hash for js files
  config.output.filename = `${jsSitecorePath}[name].js`;
  config.output.chunkFilename = `${jsSitecorePath}[name].chunk.js`;

  // Get rid of hash for css files
  const miniCssExtractPlugin = config.plugins.find(
    (element) => element.constructor.name === "MiniCssExtractPlugin"
  );
  miniCssExtractPlugin.options.filename = `${cssSitecorePath}[name].css`;
  miniCssExtractPlugin.options.chunkFilename = `${cssSitecorePath}[name].css`;

  return config;
};
