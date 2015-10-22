using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RDACcs
{
    class JournalsFilter
    {
        private SortedDictionary<String, SortedDictionary<String, HashSet<String>>> journals_options =
            new SortedDictionary<String, SortedDictionary<String, HashSet<String>>>();

        //"Application(Error[All],Warning[All])|System(Error[All],Warning[All])|Security(AuditFailure[0;10])"
        public void init( String options )
        {
            //boost::char_separator< char > journals_separator( "|", 0, boost::keep_empty_tokens );
            char[] journals_separator = {'|'};
            //boost::tokenizer<boost::char_separator< char > > journals_tokens( options_, journals_separator );
            String[] journals_tokens = options.Split(journals_separator);
            //for ( boost::tokenizer< boost::char_separator< char > >::iterator journals_tokens_iter = journals_tokens.begin();  
            //                                                                  journals_tokens_iter != journals_tokens.end(); 
            //                                                                ++journals_tokens_iter ){
            foreach (String journals_tokens_iter in journals_tokens)
            {
                //    const std::string::size_type journals_position( journals_tokens_iter->find( "(" ) );
                Int32 journals_position = journals_tokens_iter.IndexOf('(');
                //    if( journals_position == std::string::npos ){
                //        throw std::runtime_error( "Invalid arguments" );
                //    }
                if (journals_position == -1)
                {
                    throw new Exception("Invalid arguments");
                }
                //    const std::string journal_name( boost::algorithm::trim_copy( journals_tokens_iter->substr( 0, journals_position ) ) );
                String journal_name = journals_tokens_iter.Substring(0, journals_position).Trim();
                //    std::map< std::string, std::set< std::string > >current_levels;
                SortedDictionary<String, HashSet<String>> current_levels = new SortedDictionary<String, HashSet<String>>();
                //    {
                {
                    //        const std::string level( boost::algorithm::trim_copy( journals_tokens_iter->substr( ( journals_position + 1 ), ( journals_tokens_iter->length() - ( journals_position + 2 ) ) ) ) );
                    String level = journals_tokens_iter.Substring((journals_position + 1), (journals_tokens_iter.Length - (journals_position + 2))).Trim();
                    //        boost::char_separator< char > level_separator( ",", 0, boost::keep_empty_tokens );
                    char[] level_separator = { ',' };
                    //        boost::tokenizer<boost::char_separator< char > > level_tokens( level, level_separator );
                    String[] level_tokens = level.Split(level_separator);
                    //        for ( boost::tokenizer< boost::char_separator< char > >::iterator level_tokens_iter = level_tokens.begin();  
                    //                                                                          level_tokens_iter != level_tokens.end(); 
                    //                                                                        ++level_tokens_iter ){
                    foreach (String level_tokens_iter in level_tokens)
                    {
                        //            const std::string::size_type level_position( level_tokens_iter->find( "[" ) );
                        Int32 level_position = level_tokens_iter.IndexOf('[');
                        //            if( level_position == std::string::npos ){
                        //                throw std::runtime_error( "Invalid arguments" );
                        //            }
                        if (level_position == -1)
                        {
                            throw new Exception("Invalid arguments");
                        }
                        //            const std::string level_name( boost::algorithm::trim_copy( level_tokens_iter->substr( 0, level_position ) ) );
                        String level_name = level_tokens_iter.Substring(0, level_position).Trim();
                        //            std::set< std::string >current_codes;
                        HashSet<String> current_codes = new HashSet<String>();
                        //            {
                        {
                            //                const std::string code( boost::algorithm::trim_copy( level_tokens_iter->substr( ( level_position + 1 ), ( level_tokens_iter->length() - ( level_position + 2 ) ) ) ) );
                            String code = level_tokens_iter.Substring((level_position + 1), (level_tokens_iter.Length - (level_position + 2))).Trim();
                            //                boost::char_separator< char > code_separator( ";", 0, boost::keep_empty_tokens );
                            char[] code_separator = { ';' };
                            //                boost::tokenizer<boost::char_separator< char > > code_tokens( code, code_separator );
                            String[] code_tokens = code.Split(code_separator);
                            //                for ( boost::tokenizer< boost::char_separator< char > >::iterator code_tokens_iter = code_tokens.begin();  
                            //                                                                                  code_tokens_iter != code_tokens.end(); 
                            //                                                                                ++code_tokens_iter ){
                            foreach (String code_tokens_iter in code_tokens)
                            {
                                //                    const std::string code_name( boost::algorithm::trim_copy( *code_tokens_iter ) );
                                String code_name = code_tokens_iter.Trim();
                                //                    if( code_name.empty() ){
                                //                        throw std::runtime_error( "Invalid arguments" );
                                //                    }
                                if (code_name.Length == 0)
                                {
                                    throw new Exception("Invalid arguments");
                                }
                                //                    current_codes.insert( code_name );
                                current_codes.Add(code_name);
                            //                }
                            }
                        //            }
                        }
                        //            current_levels[ level_name.c_str() ] = current_codes;
                        current_levels[level_name] = current_codes;
                    //        }
                    }
                //    }
                }
                //    journals_options[ journal_name.c_str() ] = current_levels;
                journals_options[journal_name] = current_levels;
            //}
            }
        }

		public bool isExist( String journal_, 
							 String level_, 
							 String code_  )
        {
            //const std::map< std::string, std::map< std::string, std::set< std::string > > >::const_iterator journal_positions( journals_options.find( journal_ ) );
            Boolean journal_positions = journals_options.ContainsKey(journal_);
            //if( journal_positions == journals_options.end() ){
            //    return false;
            //}
            if (!journal_positions)
            {
                return false;
            }
            //const std::map< std::string, std::set< std::string > >::const_iterator level_positions( journal_positions->second.find( level_ ) );
            Boolean level_positions = journals_options[journal_].ContainsKey(level_);
            //if( level_positions == journal_positions->second.end() ){
            //    return false;
            //}
            if (!level_positions)
            {
                return false;
            }
            //const std::set< std::string >::const_iterator code_positions_all( level_positions->second.find( "All" ) );
            Boolean code_positions_all = journals_options[journal_][level_].Contains("All");
            //const std::set< std::string >::const_iterator code_positions( level_positions->second.find( code_ ) );
            Boolean code_positions = journals_options[journal_][level_].Contains(code_);
            //if( code_positions_all == level_positions->second.end() && 
            //    code_positions == level_positions->second.end() ){
            //    return false;
            //}
            if (!code_positions_all &&
                !code_positions)
            {
                return false;
            }

            //return true;
            return true;
        }

		public List< String > getJournals()
        {
            //std::vector< std::string > result;
            List<String> result = new List<String>();
            String[]keys = new String[journals_options.Keys.Count];
            journals_options.Keys.CopyTo(keys, 0);
            //for(const auto &iter: journals_options){
            foreach (String iter in keys)
            {
                //    result.push_back( iter.first );	
                result.Add(iter);
            //}
            }
            //return result;
            return result;
        }


    }
}
