import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';

Future<void> openExternalLink(BuildContext context, String link) async {
  final uri = Uri.tryParse(link);
  if (uri == null ||
      !uri.hasAuthority ||
      !{'http', 'https'}.contains(uri.scheme.toLowerCase())) {
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Link must be a valid http or https URL.')),
    );
    return;
  }
  if (!await launchUrl(uri, mode: LaunchMode.externalApplication) &&
      context.mounted) {
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(const SnackBar(content: Text('Could not open link.')));
  }
}
